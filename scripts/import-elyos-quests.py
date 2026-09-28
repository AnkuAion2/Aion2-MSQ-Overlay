"""Import English Elyos Story pages into a review candidate, never application data.
Uses only the Python standard library. Cached HTML is audit material, not release data.
Run from the repository root: python scripts/import-elyos-quests.py
"""
import argparse
import datetime
import hashlib
import json
import re
import time
import urllib.request
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urljoin

BASE = "https://aion2.app"
LIST = BASE + "/db/quests?race=Light&type=Hero"
class Node:
    def __init__(self, tag="", attrs=()):
        self.tag, self.attrs, self.children = tag, dict(attrs), []
    def text(self):
        return "".join(c if isinstance(c,str) else c.text() for c in self.children).strip()
    def find(self, tag):
        for c in self.children:
            if isinstance(c,Node):
                if c.tag == tag: yield c
                yield from c.find(tag)
class Tree(HTMLParser):
    def __init__(self, html):
        super().__init__(convert_charrefs=True)
        self.root=Node(); self.stack=[self.root]; self.feed(html)
    def handle_starttag(self, tag, attrs):
        n=Node(tag,attrs); self.stack[-1].children.append(n)
        if tag not in {"area","base","br","col","embed","hr","img","input","link","meta","param","source","track","wbr"}:
            self.stack.append(n)
    def handle_endtag(self,tag):
        for i in range(len(self.stack)-1,0,-1):
            if self.stack[i].tag==tag:
                del self.stack[i:]; break
    def handle_data(self,data): self.stack[-1].children.append(data)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--cache",default="artifacts/elyos-import")
    parser.add_argument("--output",default="artifacts/elyos-import/elyos-candidate.json")
    args=parser.parse_args()
    data_root = (Path(__file__).resolve().parents[1] / "AionSpeedrunOverlay" / "Data").resolve()
    out = Path(args.output).resolve()
    provenance = out.with_suffix(".provenance.json")
    cache = Path(args.cache).resolve()
    for destination in (out, provenance, cache):
        if destination == data_root or data_root in destination.parents:
            parser.error("Application Data is protected; use an artifacts review destination.")
    if out == provenance:
        parser.error("Choose a candidate filename without the .provenance.json suffix.")
    if "asmodian" in out.name.lower():
        parser.error("Refusing an Asmodian output filename.")
    for destination in (out, provenance):
        if destination.exists():
            parser.error(f"Output already exists: {destination}. Choose a new --output path.")
    cache.mkdir(parents=True, exist_ok=True)
    evidence=[]
    def get(url,name):
        path=cache/name
        if not path.exists():
            request=urllib.request.Request(url,headers={"User-Agent":"AionSpeedrunOverlay-catalog-import/1.0"})
            with urllib.request.urlopen(request,timeout=30) as response: raw=response.read()
            path.write_bytes(raw); time.sleep(.2)
        raw=path.read_bytes()
        evidence.append({"url":url,"file":name,"sha256":hashlib.sha256(raw).hexdigest()})
        return Tree(raw.decode("utf-8")).root
    links=[]; url=LIST; page=1; visited=set()
    while url:
        if url in visited: raise ValueError("Pagination loop")
        visited.add(url)
        root=get(url,f"list-{page}.html")
        for a in root.find("a"):
            href=a.attrs.get("href","")
            if re.fullmatch(r"/db/quests/\d+",href):
                # List cards provide the level; check details independently below.
                level=re.search(r"Lv\.?\s*(\d+)",a.text())
                if not level: raise ValueError("Missing level: "+href)
                if int(level[1])<=45 and href not in links: links.append(href)
        next_links=[a for a in root.find("a") if a.text()=="Next"]
        url=urljoin(BASE,next_links[0].attrs["href"]) if next_links else None
        page+=1
    if not links: raise ValueError("No Elyos quests found")
    quests=[]; next_ids={}
    for index,href in enumerate(links):
        qid=href.rsplit("/",1)[1]
        root=get(BASE+href,qid+".html")
        heading=next(root.find("h1"))
        headers=[h for h in root.find("header") if any(x is heading for x in h.find("h1"))]
        if len(headers)!=1: raise ValueError("Missing quest header "+qid)
        header=headers[0].text()
        if "Elyos" not in header or "Story" not in header: raise ValueError("Wrong faction/type "+qid)
        level=int(re.search(r"Lv\s*(\d+)",header)[1])
        if not 1<=level<=45: raise ValueError("Unexpected level "+qid)
        chapter=re.search(r"Chapter\s*:\s*(.+)",header)[1].strip()
        sections=list(root.find("section"))
        objectives=[p.text() for sec in sections if any(s.text()=="Objective" for s in sec.find("span")) for p in sec.find("p")]
        steps_sections=[sec for sec in sections if any(h.text()=="Steps" for h in sec.find("h2"))]
        if len(objectives)!=1 or len(steps_sections)!=1: raise ValueError("Missing objectives/steps "+qid)
        steps=[]; used=set()
        for li in steps_sections[0].find("li"):
            blocks=[s.text() for s in li.find("span") if "block" in s.attrs.get("class","").split()]
            if not blocks or not blocks[0]: raise ValueError("Empty step "+qid)
            title=blocks[0]
            slug=re.sub(r"[^a-z0-9]+","-",title.lower()).strip("-")
            sid=slug; suffix=2
            while sid in used: sid=f"{slug}-{suffix}"; suffix+=1
            used.add(sid)
            targets=[s.removeprefix("•").strip() for s in blocks[1:]]
            steps.append({"id":sid,"title":title,"objectiveAliases":targets,"notes":[]})
        if not steps: raise ValueError("Empty steps "+qid)
        quests.append({"id":qid,"recognitionKind":"elyos","recommendedLevel":level,
            "allowWithoutLevel":qid=="1100010","chapter":chapter,"title":heading.text(),
            "aliases":[heading.text()],"objectiveAliases":objectives,"notes":[],"steps":steps,
            "sourceUrl":BASE+href})
        for nav in root.find("nav"):
            for a in nav.find("a"):
                if "Next" in a.text() and re.fullmatch(r"/db/quests/\d+",a.attrs.get("href","")):
                    next_ids[qid]=a.attrs["href"].rsplit("/",1)[1]
        if (index+1)%10==0: print(f"Read {index+1}/{len(links)} quests",flush=True)
    by_id={q["id"]:q for q in quests}
    if len(by_id)!=len(quests): raise ValueError("Duplicate quest IDs")
    starts=[qid for qid in by_id if qid not in next_ids.values()]
    if starts!=["1100010"]: raise ValueError("Unexpected route roots: "+str(starts))
    ordered=[]; current=starts[0]
    while current in by_id and current not in [q["id"] for q in ordered]:
        ordered.append(by_id[current]); current=next_ids.get(current)
    if len(ordered)!=len(quests): raise ValueError("Incomplete/branching route; review manually")
    for i,q in enumerate(ordered,1): q["routeIndex"]=i
    doc={"schemaVersion":2,"game":"AION 2","race":"Elyos","status":"experimental",
        "maxRecommendedLevel":45,"source":{"database":"Aion2t / Aion2.app","url":LIST,
        "retrievedAtUtc":datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "note":"English public Story detail pages; names, levels, objectives and steps only. No live-client validation. Step IDs are locally generated stable slugs; do not regenerate published IDs without migration."},
        "quests":ordered}
    out.parent.mkdir(parents=True, exist_ok=True)
    # Exclusive creation also prevents overwriting if a file appears during download.
    with out.open("x", encoding="utf-8") as stream:
        json.dump(doc, stream, ensure_ascii=False, indent=2)
        stream.write(chr(10))
    with provenance.open("x", encoding="utf-8") as stream:
        json.dump(evidence, stream, indent=2)
    print(f"Review candidate: {out}; maintained catalogs were not changed.")
    print(f"Imported {len(ordered)} quests / {sum(len(q['steps']) for q in ordered)} steps; all notes empty.")
if __name__=="__main__": main()