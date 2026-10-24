import csv
import importlib.util
import tempfile
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location('selector', Path(__file__).with_name('Select-OpenImages.py'))
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)

class SelectionTests(unittest.TestCase):
    def test_face_geometry_and_flags(self):
        row = dict(XMin='0', XMax='.3', YMin='0', YMax='.3', IsOccluded='0',
                   IsTruncated='0', IsGroupOf='0', IsDepiction='0')
        self.assertAlmostEqual(module.face_area(row), .09)
        self.assertIsNone(module.face_area(dict(row, IsOccluded='1')))
        self.assertIsNone(module.face_area(dict(row, XMax='.1')))
        self.assertIsNone(module.face_area(dict(row, IsDepiction='-1')))

    def test_join_excludes_negative_labels_and_wrong_license(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); source = root/'metadata'; source.mkdir()
            def write(index, records):
                with (source/module.FILES[index]).open('w', newline='', encoding='utf-8') as stream:
                    writer = csv.DictWriter(stream, fieldnames=list(records[0])); writer.writeheader(); writer.writerows(records)
            write(0, [dict(LabelName=name, DisplayName=name) for name in ('Human face', *module.SCENES)])
            write(1, [dict(ImageID=i, LabelName='Human face', XMin='0', XMax='.5', YMin='0', YMax='.5',
                     IsOccluded='0', IsTruncated='0', IsGroupOf='0', IsDepiction='0') for i in ('good','negative','nc')])
            write(2, [dict(ImageID=i, LabelName='Human face', Confidence='0' if i=='negative' else '1')
                     for i in ('good','negative','nc')])
            write(3, [dict(ImageID=i, Subset='train', OriginalLandingURL='https://example.org/'+i,
                     OriginalURL='https://example.org/'+i+'.jpg', Author='Example', AuthorProfileURL='https://example.org/author',
                     License='https://creativecommons.org/licenses/'+('by-nc' if i=='nc' else 'by')+'/2.0/')
                     for i in ('good','negative','nc')])
            result = module.select(source, root/'output')
            self.assertEqual([r['ImageID'] for r in result], ['good'])
            self.assertFalse(result[0]['training_ready'])
            with self.assertRaises(ValueError): module.select(source, root/'output')
            with self.assertRaises(ValueError): module.select(source, source/'output')

if __name__ == '__main__': unittest.main()
