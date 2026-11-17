"""
Generates the Grafana dashboards of FinHorizon (an overview plus one per service) from the log fields of
FinHorizon.Observability.LogFields. Edit this file, not the JSON, then run from this directory:

    python3 generate_dashboards.py

Grafana reloads the files by itself within a few seconds.
"""
import json
from pathlib import Path

OUT = Path(__file__).parent / "dashboards"
DS = {"type": "loki", "uid": "loki"}

HTTP = '| json | Event="http_request"'
USE_CASE = '| json | Event="use_case"'


class Board:
    def __init__(self, uid, title, description, tags):
        self.uid, self.title, self.description, self.tags = uid, title, description, tags
        self.panels, self.y, self.next_id, self.variables = [], 0, 1, []

    def row(self, title):
        self.panels.append({"type": "row", "title": title, "id": self._id(), "collapsed": False, "gridPos": {"h": 1, "w": 24, "x": 0, "y": self.y}, "panels": []})
        self.y += 1
        self.x = 0
        self.row_h = 0

    def _id(self):
        self.next_id += 1
        return self.next_id

    def add(self, panel, w, h):
        if getattr(self, "x", 0) + w > 24:
            self.y += self.row_h
            self.x, self.row_h = 0, 0
        panel["id"] = self._id()
        panel["gridPos"] = {"h": h, "w": w, "x": self.x, "y": self.y}
        panel.setdefault("datasource", DS)
        self.panels.append(panel)
        self.x += w
        self.row_h = max(self.row_h, h)
        if self.x >= 24:
            self.y += self.row_h
            self.x, self.row_h = 0, 0

    def end_row(self):
        if getattr(self, "x", 0) > 0:
            self.y += self.row_h
        self.x, self.row_h = 0, 0

    def json(self):
        return {
            "uid": self.uid,
            "title": self.title,
            "description": self.description,
            "tags": self.tags,
            "editable": False,
            "schemaVersion": 41,
            "version": 1,
            "refresh": "30s",
            "time": {"from": "now-6h", "to": "now"},
            "timezone": "browser",
            "graphTooltip": 1,
            "templating": {"list": self.variables},
            "annotations": {"list": []},
            "links": [{"title": "FinHorizon", "type": "dashboards", "tags": ["finhorizon"], "asDropdown": True}],
            "panels": self.panels,
        }


def target(expr, legend="", instant=False, ref="A"):
    return {"refId": ref, "datasource": DS, "expr": expr, "legendFormat": legend, "queryType": "instant" if instant else "range"}


def stat(title, expr, unit="short", description="", thresholds=None, decimals=None):
    if unit != "ms":
        expr = f"({expr}) or vector(0)"
    steps = thresholds or [{"color": "green", "value": None}]
    defaults = {"unit": unit, "thresholds": {"mode": "absolute", "steps": steps}, "color": {"mode": "thresholds"}}
    if decimals is not None:
        defaults["decimals"] = decimals
    return {
        "type": "stat",
        "title": title,
        "description": description,
        "targets": [target(expr, instant=True)],
        "fieldConfig": {"defaults": defaults, "overrides": []},
        "options": {"reduceOptions": {"calcs": ["lastNotNull"], "fields": "", "values": False}, "colorMode": "value", "graphMode": "none", "textMode": "value"},
    }


def series(title, targets, unit="short", description="", stack=False, draw="line"):
    return {
        "type": "timeseries",
        "title": title,
        "description": description,
        "targets": targets,
        "fieldConfig": {
            "defaults": {
                "unit": unit,
                "custom": {"drawStyle": draw, "fillOpacity": 25 if stack else 10, "lineWidth": 1, "stacking": {"mode": "normal" if stack else "none"}, "showPoints": "never"},
            },
            "overrides": [],
        },
        "options": {"legend": {"displayMode": "table", "placement": "right", "calcs": ["sum"] if unit == "short" else ["mean", "max"]}, "tooltip": {"mode": "multi", "sort": "desc"}},
    }


def bars(title, expr, legend, unit="short", description=""):
    return {
        "type": "bargauge",
        "title": title,
        "description": description,
        "targets": [target(expr, legend)],
        "maxDataPoints": 1,
        "fieldConfig": {"defaults": {"unit": unit, "color": {"mode": "continuous-GrYlRd"}, "min": 0}, "overrides": []},
        "options": {"orientation": "horizontal", "displayMode": "gradient", "showUnfilled": True, "reduceOptions": {"calcs": ["lastNotNull"], "fields": "", "values": False}, "valueMode": "color", "namePlacement": "left", "sizing": "auto"},
    }


def logs(title, expr, description=""):
    return {
        "type": "logs",
        "title": title,
        "description": description,
        "targets": [target(expr)],
        "options": {"showTime": True, "wrapLogMessage": True, "prettifyLogMessage": False, "enableLogDetails": True, "sortOrder": "Descending", "dedupStrategy": "none"},
    }


STATUS_CLASS = '| label_format StatusClass="{{ substr 0 1 .StatusCode }}xx"'
ERROR_STEPS = [{"color": "green", "value": None}, {"color": "orange", "value": 1}, {"color": "red", "value": 5}]
LATENCY_STEPS = [{"color": "green", "value": None}, {"color": "orange", "value": 500}, {"color": "red", "value": 2000}]


def text_variable(name, label, description):
    return {"type": "textbox", "name": name, "label": label, "description": description, "query": "", "current": {"value": "", "text": ""}, "hide": 0}


def http_section(board, sel, service_title):
    board.row("Traffico HTTP")
    board.add(stat("Richieste", f"sum(count_over_time({sel} {HTTP} [$__range]))", description="Richieste HTTP concluse nell'intervallo (una riga per richiesta, RequestLoggingMiddleware)."), 4, 4)
    board.add(stat("Errori 5xx", f'100 * (sum(count_over_time({sel} {HTTP} | StatusCode=~"5.." [$__range])) or vector(0)) / sum(count_over_time({sel} {HTTP} [$__range]))', unit="percent", decimals=2, thresholds=ERROR_STEPS, description="Quota di richieste con errore del server."), 4, 4)
    board.add(stat("Errori 4xx", f'100 * (sum(count_over_time({sel} {HTTP} | StatusCode=~"4.." [$__range])) or vector(0)) / sum(count_over_time({sel} {HTTP} [$__range]))', unit="percent", decimals=2, description="Quota di richieste rifiutate: dati non validi, non trovato, accesso negato."), 4, 4)
    board.add(stat("Latenza p95", f"quantile_over_time(0.95, {sel} {HTTP} | unwrap ElapsedMs | __error__=\"\" [$__range]) by (service)", unit="ms", thresholds=LATENCY_STEPS), 4, 4)
    board.add(stat("Utenti attivi", f'count(sum by (UserId) (count_over_time({sel} {HTTP} | UserId!="" [$__range])))', description="Utenti distinti (per id pseudonimo, il sub di Keycloak) con almeno una richiesta."), 4, 4)
    board.add(stat("Richieste negate (401/403)", f'sum(count_over_time({sel} {HTTP} | StatusCode=~"401|403" [$__range]))', thresholds=[{"color": "green", "value": None}, {"color": "orange", "value": 10}]), 4, 4)
    board.add(series("Richieste per classe di stato", [target(f"sum by (StatusClass) (count_over_time({sel} {HTTP} {STATUS_CLASS} [$__interval]))", "{{StatusClass}}")], stack=True, draw="bars"), 12, 8)
    board.add(series("Latenza p95 per rotta", [target(f"quantile_over_time(0.95, {sel} {HTTP} | unwrap ElapsedMs | __error__=\"\" [$__interval]) by (Method, Route)", "{{Method}} {{Route}}")], unit="ms"), 12, 8)
    board.add(bars("Rotte più chiamate", f"topk(15, sum by (Method, Route) (count_over_time({sel} {HTTP} [$__range])))", "{{Method}} {{Route}}"), 12, 9)
    board.add(bars("Rotte con più errori", f'topk(15, sum by (Method, Route, StatusCode) (count_over_time({sel} {HTTP} | StatusCode=~"[45].." [$__range])))', "{{StatusCode}} {{Method}} {{Route}}"), 12, 9)
    board.end_row()


def logs_section(board, sel):
    board.row("Log")
    board.add(series("Righe di log per livello", [target(f"sum by (level) (count_over_time({sel} [$__interval]))", "{{level}}")], stack=True, draw="bars"), 12, 8)
    board.add(bars("Avvisi ed errori più frequenti", f'topk(10, sum by (logger, messageTemplate) (count_over_time({sel} | level=~"WARN|ERROR|FATAL" | json [$__range])))', "{{logger}}: {{messageTemplate}}"), 12, 8)
    board.add(logs("Errori e avvisi", f'{sel} | level=~"WARN|ERROR|FATAL"'), 24, 10)
    board.add(logs("Tutti i log (filtri: testo e utente)", f'{sel} |= "$search" | json | UserId=~"$user.*"', description="Il filtro utente usa l'id pseudonimo (sub di Keycloak): mostra tutto quello che quell'utente ha fatto."), 24, 12)
    board.end_row()


def service_board(uid, service, title, description, specific):
    board = Board(uid, title, description, ["finhorizon", service])
    board.variables = [
        text_variable("search", "Testo", "Filtra le righe di log che contengono questo testo."),
        text_variable("user", "Utente (id)", "Id pseudonimo dell'utente (sub di Keycloak)."),
    ]
    sel = f'{{service="{service}"}}'
    http_section(board, sel, title)
    specific(board, sel)
    logs_section(board, sel)
    return board


def mysync_specific(board, sel):
    board.row("Casi d'uso")
    board.add(series("Casi d'uso per esito", [target(f"sum by (Status) (count_over_time({sel} {USE_CASE} [$__interval]))", "{{Status}}")], stack=True, draw="bars", description="Ok e Created riusciti, Invalid rifiutati per dati non validi, NotFound per record inesistenti o di un altro utente."), 12, 8)
    board.add(series("Durata p95 dei casi d'uso", [target(f"quantile_over_time(0.95, {sel} {USE_CASE} | unwrap ElapsedMs | __error__=\"\" [$__interval]) by (UseCase)", "{{UseCase}}")], unit="ms"), 12, 8)
    board.add(bars("Casi d'uso più eseguiti", f"topk(15, sum by (UseCase) (count_over_time({sel} {USE_CASE} [$__range])))", "{{UseCase}}"), 8, 9)
    board.add(bars("Casi d'uso rifiutati", f'topk(15, sum by (UseCase) (count_over_time({sel} {USE_CASE} | Status="Invalid" [$__range])))', "{{UseCase}}", description="Rifiuti per dati non validi: indicano dove l'interfaccia non guida abbastanza l'utente."), 8, 9)
    board.add(bars("Campi rifiutati più spesso", f'topk(15, sum by (UseCase, Fields) (count_over_time({sel} {USE_CASE} | Status="Invalid" [$__range])))', "{{UseCase}} · {{Fields}}"), 8, 9)
    board.end_row()
    board.row("Dipendenze")
    board.add(stat("Servizio valute non disponibile", f'sum(count_over_time({sel} {HTTP} | StatusCode="503" [$__range]))', thresholds=[{"color": "green", "value": None}, {"color": "red", "value": 1}], description="Risposte 503: tassi di cambio di Frankfurter non disponibili (UC13-5a)."), 6, 4)
    board.add(stat("Abbonamenti: addebiti registrati", f'sum(count_over_time({sel} {USE_CASE} | UseCase="RecordTransactionCommand" | Status="Created" [$__range]))', description="Movimenti registrati (spese, ricavi, trasferimenti), compresi i pagamenti degli abbonamenti."), 6, 4)
    board.add(stat("Abbonamenti creati", f'sum(count_over_time({sel} {USE_CASE} | UseCase="CreateSubscriptionCommand" | Status="Created" [$__range]))'), 6, 4)
    board.add(stat("Abbonamenti annullati", f'sum(count_over_time({sel} {USE_CASE} | UseCase="CancelSubscriptionCommand" | Status="Ok" [$__range]))'), 6, 4)
    board.add(logs("Migrazioni e avvio", f'{sel} |~ "(?i)migrat|Application started|stopped because"'), 24, 7)
    board.end_row()


def adminportal_specific(board, sel):
    board.row("Operazioni del portale")
    board.add(series("Eventi per componente", [target(f'sum by (logger) (count_over_time({sel} | json | Event!="http_request" [$__interval]))', "{{logger}}")], stack=True, draw="bars", description="Righe scritte dai servizi del portale (aziende, comunicazioni, catalogo, fatturazione), escluse le richieste HTTP."), 12, 8)
    board.add(bars("Operazioni sulle aziende", f'sum by (messageTemplate) (count_over_time({sel} | json | logger=~".*Tenant.*" [$__range]))', "{{messageTemplate}}"), 12, 8)
    board.add(stat("Email inviate", f'sum(count_over_time({sel} | json | messageTemplate="Email sent through {{Channel}}" [$__range]))'), 6, 4)
    board.add(stat("Utenti non raggiungibili (Keycloak)", f'sum(count_over_time({sel} {HTTP} | StatusCode="503" [$__range]))', thresholds=[{"color": "green", "value": None}, {"color": "red", "value": 1}], description="Risposte 503: directory utenti o impersonazione su Keycloak non disponibili."), 6, 4)
    board.add(stat("Moduli non raggiungibili", f'sum(count_over_time({sel} | json | logger=~".*ModuleProvisioner.*" | level="WARN" [$__range]))', thresholds=[{"color": "green", "value": None}, {"color": "orange", "value": 1}]), 6, 4)
    board.add(stat("Conflitti di unicità", f'sum(count_over_time({sel} {HTTP} | StatusCode="409" [$__range]))', description="Azienda o unità produttiva già esistente (409)."), 6, 4)
    board.end_row()
    board.row("Job in background")
    board.add(logs("Fatturazione e riprova provisioning", f'{sel} | json | logger=~".*(BillingJob|ProvisioningRetryJob|BillingService).*"'), 24, 9)
    board.end_row()


def overview():
    board = Board("finhorizon-overview", "FinHorizon – Panoramica", "Tutti i servizi a colpo d'occhio: traffico, errori, latenza e log.", ["finhorizon"])
    sel = '{service=~".+"}'
    board.row("Servizi")
    board.add(stat("Richieste", f"sum(count_over_time({sel} {HTTP} [$__range]))"), 6, 4)
    board.add(stat("Errori 5xx", f'100 * (sum(count_over_time({sel} {HTTP} | StatusCode=~"5.." [$__range])) or vector(0)) / sum(count_over_time({sel} {HTTP} [$__range]))', unit="percent", decimals=2, thresholds=ERROR_STEPS), 6, 4)
    board.add(stat("Utenti attivi", f'count(sum by (UserId) (count_over_time({sel} {HTTP} | UserId!="" [$__range])))'), 6, 4)
    board.add(stat("Errori nei log", f'sum(count_over_time({{service=~".+", level=~"ERROR|FATAL"}} [$__range]))', thresholds=[{"color": "green", "value": None}, {"color": "red", "value": 1}]), 6, 4)
    board.add(series("Richieste per servizio", [target(f"sum by (service) (count_over_time({sel} {HTTP} [$__interval]))", "{{service}}")], stack=True, draw="bars"), 12, 8)
    board.add(series("Errori 5xx per servizio", [target(f'sum by (service) (count_over_time({sel} {HTTP} | StatusCode=~"5.." [$__interval]))', "{{service}}")], draw="bars"), 12, 8)
    board.add(series("Latenza p95 per servizio", [target(f"quantile_over_time(0.95, {sel} {HTTP} | unwrap ElapsedMs | __error__=\"\" [$__interval]) by (service)", "{{service}}")], unit="ms"), 12, 8)
    board.add(series("Log per servizio e livello", [target(f"sum by (service, level) (count_over_time({sel} [$__interval]))", "{{service}} {{level}}")], stack=True, draw="bars"), 12, 8)
    board.add(logs("Errori di tutti i servizi", '{service=~".+", level=~"ERROR|FATAL"}'), 24, 10)
    board.end_row()
    board.panels.insert(0, {
        "type": "text", "title": "", "id": 1, "gridPos": {"h": 3, "w": 24, "x": 0, "y": 0},
        "options": {"mode": "markdown", "content": "Dashboard dei servizi: [MySync backend](/d/finhorizon-mysync-backend) · [Admin Portal backend](/d/finhorizon-adminportal-backend). Ogni riga di log ha l'utente come id pseudonimo (`UserId`, sub di Keycloak), mai nome o email."},
    })
    for panel in board.panels[1:]:
        panel["gridPos"]["y"] += 3
    return board


boards = {
    "finhorizon-overview.json": overview(),
    "mysync-backend.json": service_board("finhorizon-mysync-backend", "mysync-backend", "MySync – Backend", "Traffico, casi d'uso, dipendenze e log del backend di MySync.", mysync_specific),
    "adminportal-backend.json": service_board("finhorizon-adminportal-backend", "adminportal-backend", "Admin Portal – Backend", "Traffico, operazioni del portale, job e log del backend dell'Admin Portal.", adminportal_specific),
}
for name, board in boards.items():
    (OUT / name).write_text(json.dumps(board.json(), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(name, len(board.panels), "panels")
