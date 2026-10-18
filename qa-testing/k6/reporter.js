/**
 * Standalone Zero-Dependency HTML Dashboard Reporter for k6
 */
export function generateHtmlReport(data, title = 'CeylonQuest k6 Performance Report') {
  const metrics = data.metrics;
  const p95 = metrics.http_req_duration ? (metrics.http_req_duration.values['p(95)']).toFixed(2) : 'N/A';
  const avg = metrics.http_req_duration ? (metrics.http_req_duration.values.avg).toFixed(2) : 'N/A';
  const med = metrics.http_req_duration ? (metrics.http_req_duration.values.med).toFixed(2) : 'N/A';
  const min = metrics.http_req_duration ? (metrics.http_req_duration.values.min).toFixed(2) : 'N/A';
  const max = metrics.http_req_duration ? (metrics.http_req_duration.values.max).toFixed(2) : 'N/A';
  const totalReqs = metrics.http_reqs ? metrics.http_reqs.values.count : 0;
  const reqRate = metrics.http_reqs ? (metrics.http_reqs.values.rate).toFixed(2) : 0;
  const failRate = metrics.http_req_failed ? ((metrics.http_req_failed.values.rate) * 100).toFixed(2) : '0.00';
  const checksPass = metrics.checks ? metrics.checks.values.passes : 0;
  const checksFail = metrics.checks ? metrics.checks.values.fails : 0;
  const checksTotal = checksPass + checksFail;
  const checkRate = checksTotal > 0 ? ((checksPass / checksTotal) * 100).toFixed(1) : 100;

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <title>${title}</title>
  <style>
    :root {
      --bg: #0f172a;
      --card-bg: #1e293b;
      --border: #334155;
      --text: #f8fafc;
      --text-muted: #94a3b8;
      --accent: #38bdf8;
      --green: #22c55e;
      --red: #ef4444;
      --yellow: #f59e0b;
    }
    body {
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
      background: var(--bg);
      color: var(--text);
      margin: 0;
      padding: 2rem;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      border-bottom: 1px solid var(--border);
      padding-bottom: 1.5rem;
      margin-bottom: 2rem;
    }
    .title { font-size: 1.8rem; font-weight: 700; color: var(--accent); }
    .badge-pass { background: rgba(34, 197, 94, 0.2); color: var(--green); padding: 0.35rem 0.8rem; border-radius: 9999px; font-weight: 600; font-size: 0.9rem; border: 1px solid var(--green); }
    .badge-fail { background: rgba(239, 68, 68, 0.2); color: var(--red); padding: 0.35rem 0.8rem; border-radius: 9999px; font-weight: 600; font-size: 0.9rem; border: 1px solid var(--red); }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 2rem; }
    .card { background: var(--card-bg); border: 1px solid var(--border); border-radius: 0.75rem; padding: 1.25rem; }
    .card-label { font-size: 0.85rem; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 0.5rem; }
    .card-value { font-size: 1.8rem; font-weight: 700; }
    .table-container { background: var(--card-bg); border: 1px solid var(--border); border-radius: 0.75rem; overflow: hidden; }
    table { width: 100%; border-collapse: collapse; text-align: left; }
    th, td { padding: 1rem 1.25rem; border-bottom: 1px solid var(--border); }
    th { background: #162032; color: var(--accent); font-size: 0.85rem; text-transform: uppercase; }
    tr:last-child td { border-bottom: none; }
  </style>
</head>
<body>
  <div class="header">
    <div>
      <div class="title">${title}</div>
      <div style="color: var(--text-muted); font-size: 0.9rem; margin-top: 0.25rem;">CeylonQuest Microservice Load & Performance Benchmark</div>
    </div>
    <div class="${failRate == 0 ? 'badge-pass' : 'badge-fail'}">
      ${failRate == 0 ? 'STATUS: PASSED (100% HEALTHY)' : 'STATUS: ALERT (Thresholds Crossed)'}
    </div>
  </div>

  <div class="grid">
    <div class="card">
      <div class="card-label">Total Requests</div>
      <div class="card-value" style="color: var(--accent);">${totalReqs.toLocaleString()}</div>
    </div>
    <div class="card">
      <div class="card-label">Throughput</div>
      <div class="card-value">${reqRate} <span style="font-size: 0.9rem; color: var(--text-muted);">req/s</span></div>
    </div>
    <div class="card">
      <div class="card-label">Avg Response Time</div>
      <div class="card-value">${avg} <span style="font-size: 0.9rem; color: var(--text-muted);">ms</span></div>
    </div>
    <div class="card">
      <div class="card-label">p(95) Response Time</div>
      <div class="card-value" style="color: var(--yellow);">${p95} <span style="font-size: 0.9rem; color: var(--text-muted);">ms</span></div>
    </div>
    <div class="card">
      <div class="card-label">Error Rate</div>
      <div class="card-value" style="color: ${failRate == 0 ? 'var(--green)' : 'var(--red)'};">${failRate}%</div>
    </div>
    <div class="card">
      <div class="card-label">Checks Passed</div>
      <div class="card-value" style="color: var(--green);">${checkRate}%</div>
    </div>
  </div>

  <div class="table-container">
    <table>
      <thead>
        <tr>
          <th>Metric Name</th>
          <th>Min</th>
          <th>Median</th>
          <th>Average</th>
          <th>p(95) SLA</th>
          <th>Max</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><strong>HTTP Request Duration</strong></td>
          <td>${min} ms</td>
          <td>${med} ms</td>
          <td>${avg} ms</td>
          <td style="color: var(--yellow); font-weight: 600;">${p95} ms</td>
          <td>${max} ms</td>
        </tr>
      </tbody>
    </table>
  </div>
</body>
</html>`;
}
