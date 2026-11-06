from pathlib import Path
from pdf2image import convert_from_path
from pypdf import PdfReader
import json
import sys

ROOT=Path(__file__).resolve().parents[1]
POPLER=r'C:\Users\aldik\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\poppler\Library\bin'
summary=[]
for pdf in sorted((ROOT/'Docs/Render').glob('*/*.pdf')):
    if len(sys.argv)>1 and pdf.stem[-2:] not in sys.argv[1:] and pdf.stem not in sys.argv[1:]:continue
    pages=convert_from_path(str(pdf),dpi=110,poppler_path=POPLER,fmt='png',thread_count=2)
    reader=PdfReader(str(pdf))
    for i,im in enumerate(pages,1):
        im.save(pdf.parent/f'page-{i}.png')
        text=reader.pages[i-1].extract_text()
        summary.append({'doc':pdf.stem,'page':i,'characters':len(text),'first':text[:110],'last':text[-220:]})
(ROOT/'Logs/report-layout.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print('Rendered',len(summary),'pages')
