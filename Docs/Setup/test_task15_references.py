"""Semantic scanner regressions, independent of Unity licensing. Not gameplay tests."""
import json
from pathlib import Path
import tempfile
import unittest
import Task15References as audit

GUID = "1234567890abcdef1234567890abcdef"


class ReferenceSemantics(unittest.TestCase):
    def test_documentation_strings_and_guids_are_not_dependencies(self):
        text = 'm_Text: "guid: ' + GUID + '"\n' + 'm_Text: "{fileID: 1, guid: ' + GUID + ', type: 2}"\n'
        self.assertEqual([], list(audit.pointers(text)))

    def test_wrapped_and_list_pointers_keep_owner_identity(self):
        text = '--- !u!114 &-12\n  dependency: {fileID: 11400000, guid: ' + GUID + ',\n    type: 2}\n  - {fileID: 7, guid: ' + GUID + ', type: 2}\n'
        refs = list(audit.pointers(text))
        self.assertEqual([11400000, 7], [v['fileID'] for v in refs])
        self.assertTrue(all(v['ownerFileID'] == -12 for v in refs))

    def test_missing_reference_stays_failure_even_when_builtin_looking(self):
        for guid in [GUID, '00000000000000001234567890abcdef']:
            self.assertEqual('UNRESOLVED_GUID', audit.resolve({'guid': guid, 'fileID': 1, 'type': 0}, {})['resolution'])

    def test_builtin_container_does_not_certify_arbitrary_fileid_or_type(self):
        guid = next(iter(audit.BUILTIN_CONTAINERS))
        self.assertEqual('BUILTIN_ID_PENDING', audit.resolve({'guid': guid, 'fileID': 987654321, 'type': 0}, {})['resolution'])
        self.assertEqual('UNRESOLVED_GUID', audit.resolve({'guid': guid, 'fileID': 10202, 'type': 3}, {})['resolution'])

    def test_matching_guid_does_not_hide_invalid_text_subasset(self):
        with tempfile.TemporaryDirectory(prefix='task15-fixture-') as d:
            p = Path(d) / 'profile.asset'; p.write_text('--- !u!114 &11400000\nMonoBehaviour:\n')
            base = {'guid': GUID, 'type': 2, 'property': 'profile'}
            self.assertEqual('TEXT_ID_PRESENT', audit.resolve(dict(base, fileID=11400000), {GUID: p})['resolution'])
            self.assertEqual('INVALID_FILE_ID', audit.resolve(dict(base, fileID=-9), {GUID: p})['resolution'])

    def test_prefab_handle_exception_is_limited_to_importer_owned_handle(self):
        with tempfile.TemporaryDirectory(prefix='task15-fixture-') as d:
            p = Path(d) / 'player.prefab'; p.write_text('--- !u!1 &123\nGameObject:\n')
            base = dict(guid=GUID, type=3, property='m_SourcePrefab', ownerClassID=1001, fileID=100100000)
            self.assertEqual('IMPORT_ID_PENDING', audit.resolve(base, {GUID: p})['resolution'])
            self.assertEqual('INVALID_FILE_ID', audit.resolve(dict(base, property='requiredComponent'), {GUID: p})['resolution'])
            self.assertEqual('INVALID_FILE_ID', audit.resolve(dict(base, fileID=100100001), {GUID: p})['resolution'])

    def test_available_guid_without_asset_is_dangling_not_resolved(self):
        with tempfile.TemporaryDirectory(prefix='task15-fixture-') as d:
            root = Path(d); (root/'Assets').mkdir()
            (root/'Assets/missing.asset.meta').write_text('guid: ' + GUID + '\n')
            index, duplicates, dangling = audit.index_metas(root, [])
            self.assertNotIn(GUID, index)
            self.assertEqual(1, len(dangling))

    def test_unimported_package_sample_cannot_satisfy_required_reference(self):
        with tempfile.TemporaryDirectory(prefix='task15-fixture-') as d:
            root = Path(d); (root/'Assets').mkdir()
            package = root/'Packages/com.example.local'; sample = package/'Samples~'; sample.mkdir(parents=True)
            (sample/'Volume.asset').write_text('--- !u!114 &11400000\n')
            (sample/'Volume.asset.meta').write_text('guid: ' + GUID + '\n')
            index, _, _ = audit.index_metas(root, [package])
            self.assertEqual('UNRESOLVED_GUID', audit.resolve({'guid': GUID, 'fileID': 11400000, 'type': 2}, index)['resolution'])

    def test_embedded_package_and_protected_scope_are_separate(self):
        with tempfile.TemporaryDirectory(prefix='task15-fixture-') as d:
            root = Path(d)
            package = root/'Packages/com.example.local'; package.mkdir(parents=True)
            (package/'package.json').write_text(json.dumps(dict(name='com.example.local', version='1.0.0')))
            (root/'Packages/packages-lock.json').write_text(json.dumps({'dependencies': {'com.example.local': {'source': 'embedded', 'version': 'file:com.example.local'}}}))
            (package/'Volume.cs').write_text('class Volume {}')
            (package/'Volume.cs.meta').write_text('guid: ' + GUID + '\n')
            recovery = root/'Assets/_Recovery'; recovery.mkdir(parents=True)
            (root/'Assets/_Recovery.meta').write_text('guid: ' + 'a'*32 + '\n')
            (recovery/'0.unity').write_text('--- !u!114 &1\n  m_Script: {fileID: 11500000, guid: ' + GUID + ', type: 3}\n  required: {fileID: 1, guid: ' + 'b'*32 + ', type: 2}\n')
            (recovery/'0.unity.meta').write_text('guid: ' + 'c'*32 + '\n')
            result = audit.scan(root, tracked=set())
            self.assertEqual('FAIL', result['result'])
            self.assertEqual(1, result['counts']['wholeWorkingTree']['missingGuidPairs'])
            self.assertEqual(0, result['counts']['tracked']['missingGuidPairs'])
            self.assertEqual(1, result['counts']['protectedRecovery']['missingGuidPairs'])
            self.assertEqual(1, result['counts']['wholeWorkingTree']['resolutions']['SCRIPT_ID_CONVENTION'])


if __name__ == '__main__':
    unittest.main(verbosity=2)
