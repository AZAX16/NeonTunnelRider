using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    public sealed class InteractionSensor : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private SpawnDirector registry;
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private ProgressionSystem progression;
        private struct Contact
        { internal TrafficLease Lease; internal float Time; internal InteractionCategory Category; internal long Sequence; }
        private Contact[] contacts=Array.Empty<Contact>();
        public PlayerController Player => player;
        public SpawnDirector Registry => registry;
        public PlayerSurvival Survival => survival;
        public ProgressionSystem Progression => progression;
        public int ContactCount { get; private set; }
        public int Capacity => contacts.Length;
        public ulong TotalContacts { get; private set; }
        internal void Initialize() { contacts=new Contact[registry.Capacity]; Clear(); }
        internal void Clear() { Array.Clear(contacts,0,contacts.Length); ContactCount=0; TotalContacts=0; }
        internal void CollectContacts()
        {
            Array.Clear(contacts,0,ContactCount); ContactCount=0;
            for (int i=0;i<registry.ActiveCount;i++)
            {
                var lease=registry.GetActive(i);
                if (!lease.IsCurrent || lease.Item.Encountered) continue;
                var item=lease.Item; float earliest=float.PositiveInfinity;
                for (int legIndex=0;legIndex<player.MotionLegCount;legIndex++)
                {
                    var leg=player.GetMotionLeg(legIndex);
                    var b0=Vector3.LerpUnclamped(item.PreviousPosition,item.CurrentPosition,leg.StartTime);
                    var b1=Vector3.LerpUnclamped(item.PreviousPosition,item.CurrentPosition,leg.EndTime);
                    if (SweptBounds.TryHit(leg.Start,leg.End,player.Configuration.PlayerHalfExtents,b0,b1,item.HalfExtents,out float time))
                        earliest=Mathf.Min(earliest,Mathf.LerpUnclamped(leg.StartTime,leg.EndTime,time));
                }
                if (!float.IsFinite(earliest)) continue;
                if (ContactCount==contacts.Length) throw new InvalidOperationException("Contact buffer overflow: increase configured capacity before Playing.");
                var contact=new Contact { Lease=lease, Time=earliest, Category=item.Category, Sequence=item.SpawnSequence };
                int at=ContactCount++;
                while (at>0 && Compare(contact,contacts[at-1])<0) { contacts[at]=contacts[at-1]; at--; }
                contacts[at]=contact;
            }
        }
        private static int Compare(Contact a,Contact b)
        { int order=a.Time.CompareTo(b.Time); if (order==0) order=a.Category.CompareTo(b.Category); return order!=0 ? order : a.Sequence.CompareTo(b.Sequence); }
        internal void DispatchContacts(RunSession session,float dt)
        {
            double start=session.Snapshot.ElapsedTime;
            registry.BeginDispatch();
            try
            {
                for (int i=0;i<ContactCount;i++)
                {
                    var contact=contacts[i]; if (!contact.Lease.IsCurrent) continue;
                    var item=contact.Lease.Item;
                    var position=Vector3.LerpUnclamped(item.PreviousPosition,item.CurrentPosition,contact.Time);
                    var encounter=new Encounter(item.Key,contact.Lease.ActivationId,item.SpawnSequence,item.Lane,
                        item.PreviousAbsoluteZ+(item.AbsoluteZ-item.PreviousAbsoluteZ)*contact.Time,position,start+(double)dt*contact.Time);
                    if (item.TryInteract(new InteractionContext(session,survival,encounter,progression))) TotalContacts++;
                }
            }
            finally { registry.EndDispatch(); Array.Clear(contacts,0,ContactCount); ContactCount=0; }
        }
    }
}
