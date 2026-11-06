from pathlib import Path
from docx import Document
from docx.oxml.ns import qn

root=Path(__file__).resolve().parents[1]
path=root/'Docs/Reports/Neonmoves_Lab_09_MoSCoW.docx'
d=Document(path)
t=d.tables[0]
order={'Must':0,'Should':1,'Could':2,'Won’t':3,"Won't":3}
rows=sorted(t.rows[1:],key=lambda r:(order[r.cells[2].text.strip()],r.cells[0].text))
for i,row in enumerate(rows,1):
    t._tbl.append(row._tr)
    for cell in row.cells:
        cell._tc.get_or_add_tcPr().find(qn('w:shd')).set(qn('w:fill'),'F1F5F8' if i%2==0 else 'FFFFFF')
try:d.save(path)
except PermissionError:
    path=path.with_name('Neonmoves_Lab_09_MoSCoW_sorted.docx')
    d.save(path)
source_path=root/'Tools/create_reports.py'
source=source_path.read_text(encoding='utf-8')
target="table(d,['ID','Требование','Приоритет'],fr,[.55,5.4,1.05],10)"
sort_line="fr.sort(key=lambda row: ({'Must':0,'Should':1,'Could':2,'Won’t':3}[row[2]],row[0]))"
if sort_line not in source:
    assert source.count(target)==1
    source_path.write_text(source.replace(target,sort_line+'\n'+target),encoding='utf-8')
assert [order[r.cells[2].text] for r in d.tables[0].rows[1:]]==sorted(order[r.cells[2].text] for r in d.tables[0].rows[1:])
(root/'Logs/report09-sorted-output.txt').write_text(path.name,encoding='utf-8')
print(path)
print([(r.cells[0].text,r.cells[2].text) for r in d.tables[0].rows[1:]])
