from pathlib import Path
from copy import deepcopy
import ast
import re
import shutil
from docx import Document
from docx.oxml.ns import qn

root=Path(__file__).resolve().parents[1]
path=root/'Docs/Reports/Neonmoves_Lab_09.docx'
shutil.copy2(path,root/'Logs/Neonmoves_Lab_09.before-priorities.docx')
document=Document(path)
priorities={'F01':'Must','F02':'Should','F03':'Must','F04':'Should','F05':'Must','F06':'Must','F07':'Must','F08':'Should','F09':'Must','F10':'Must','F11':'Must','F12':'Should'}
extra=[('F13','Система пяти достижений','Should'),('F14','Дополнительные костюмы, сцены и музыка для будущих версий','Could'),('F15','Магазин, платежи, облачный рейтинг и мультиплеер в текущей версии','Won’t')]
intro='Требования определяют состав текущей версии, дополнительные возможности и границы проекта. Основной пользовательский сценарий проходит от меню через выбор управления к турниру и сохранённому результату.'
legend=('Must — обязательные требования: без них версия не выполняет основные задачи игры.\n'
        'Should — важные улучшения: повышают удобство и разнообразие, но основной игровой цикл возможен без них.\n'
        'Could — дополнительные возможности, которые можно включить при наличии времени и ресурсов.\n'
        'Won’t — функции, сознательно исключённые из текущей версии; это не запрет на их развитие в будущем.\n'
        'Приоритет не обозначает готовность функции. Достижения относятся к Should и уже реализованы; дополнительные наборы из F14 остаются возможностью развития. Магазин, платежи и сетевые функции из F15 не входят в критерии приёмки этой сборки.')

def set_paragraph(paragraph,text):
    if paragraph.runs:
        paragraph.runs[0].text=text
        for run in list(paragraph.runs)[1:]:paragraph._p.remove(run._r)
    else:paragraph.add_run(text)

table=document.tables[0]
assert [c.text for c in table.rows[0].cells]==['ID','Требование','Приоритет']
assert len(table.rows)==13
for row in table.rows[1:]:
    id_=row.cells[0].text
    set_paragraph(row.cells[2].paragraphs[0],priorities[id_])
    if id_=='F11':set_paragraph(row.cells[1].paragraphs[0],'Сохранение результатов игры')
for values in extra:
    table._tbl.append(deepcopy(table.rows[-1]._tr))
    for cell,text in zip(table.rows[-1].cells,values):set_paragraph(cell.paragraphs[0],text)
for index,row in enumerate(table.rows[1:],1):
    for cell in row.cells:
        cell._tc.get_or_add_tcPr().find(qn('w:shd')).set(qn('w:fill'),'F1F5F8' if index%2==0 else 'FFFFFF')
for paragraph in document.paragraphs:
    if paragraph.text.startswith('Требования описывают реализованную игру'):set_paragraph(paragraph,intro)
    if paragraph.text.startswith('Must: основной цикл'):set_paragraph(paragraph,legend)
output=path.with_name('Neonmoves_Lab_09_MoSCoW.docx')
document.save(output)

# Keep the generator consistent without regenerating the other seven reports.
generator=root/'Tools/create_reports.py'
source=generator.read_text(encoding='utf-8')
match=re.search(r'^fr=(\[.*\])$',source,re.M)
rows=ast.literal_eval(match.group(1))
rows=[(id_,'Сохранение результатов игры' if id_=='F11' else title,priorities[id_]) for id_,title,_ in rows if id_ in priorities]+extra
source=source[:match.start(1)]+repr(rows)+source[match.end(1):]
source=source.replace('Требования описывают реализованную игру и критерии её проверки. Основной пользовательский сценарий проходит от меню через выбор управления к турниру и сохранённому результату.',intro)
source=re.sub(r"^p\(d,'Must: основной цикл.*$",lambda _: 'p(d,'+repr(legend)+')',source,count=1,flags=re.M)
generator.write_text(source,encoding='utf-8')
assert {row.cells[2].text for row in document.tables[0].rows[1:]}=={'Must','Should','Could','Won’t'}
print('Report 09 updated: 15 requirements, all four MoSCoW priorities.')
