import { useEffect, useState, useRef } from "react";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
} from "recharts";

const PALETTE = ["#4ade80", "#60a5fa", "#f472b6", "#fb923c", "#a78bfa", "#34d399"];

const tooltipStyle = {
  contentStyle: {
    background: "#1e293b",
    border: "none",
    borderRadius: 8,
    color: "#f1f5f9",
    fontSize: 13,
  },
};

const axisStyle = { fill: "#64748b", fontSize: 12 };

// ── Gráfico de barras vertical ──────────────────────────────────────────────
function VerticalBars({ data }) {
  return (
    <ResponsiveContainer width="100%" height={220}>
      <BarChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
        <XAxis dataKey="name" tick={axisStyle} />
        <YAxis tick={axisStyle} allowDecimals={false} />
        <Tooltip {...tooltipStyle} formatter={(v) => [`${v} usos`, "Actividad"]} />
        <Bar dataKey="value" radius={[4, 4, 0, 0]}>
          {data.map((_, i) => (
            <Cell key={i} fill={PALETTE[i % PALETTE.length]} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}

// ── Gráfico de barras horizontal ────────────────────────────────────────────
function HorizontalBars({ data }) {
  const height = Math.max(data.length * 40 + 80, 180);
  return (
    <ResponsiveContainer width="100%" height={height}>
      <BarChart data={data} layout="vertical" margin={{ top: 4, right: 16, left: 8, bottom: 4 }}>
        <XAxis type="number" tick={axisStyle} allowDecimals={false} />
        <YAxis type="category" dataKey="name" tick={axisStyle} width={110} />
        <Tooltip {...tooltipStyle} formatter={(v) => [`${v} usos`, "Actividad"]} />
        <Bar dataKey="value" radius={[0, 4, 4, 0]}>
          {data.map((_, i) => (
            <Cell key={i} fill={PALETTE[i % PALETTE.length]} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}

// ── Gráfico de donut ────────────────────────────────────────────────────────
function DonutChart({ data }) {
  const total = data.reduce((s, d) => s + d.value, 0);
  return (
    <ResponsiveContainer width="100%" height={220}>
      <PieChart>
        <Pie
          data={data}
          cx="50%"
          cy="50%"
          innerRadius={55}
          outerRadius={85}
          paddingAngle={2}
          dataKey="value"
        >
          {data.map((_, i) => (
            <Cell key={i} fill={PALETTE[i % PALETTE.length]} />
          ))}
        </Pie>
        <Tooltip
          {...tooltipStyle}
          formatter={(v, name) => [`${v} usos (${((v / total) * 100).toFixed(1)}%)`, name]}
        />
        <Legend
          formatter={(value, entry) => (
            <span style={{ color: "#94a3b8", fontSize: 12 }}>
              {value}{" "}
              <strong style={{ color: "#f1f5f9" }}>
                {((entry.payload.value / total) * 100).toFixed(0)}%
              </strong>
            </span>
          )}
        />
      </PieChart>
    </ResponsiveContainer>
  );
}

// ── Componente principal ────────────────────────────────────────────────────
const CHART_TYPES = [
  { id: "vertical", label: "Barras verticales" },
  { id: "horizontal", label: "Barras horizontales" },
  { id: "donut", label: "Donut" },
];

const btnBase = {
  padding: "4px 12px",
  borderRadius: 6,
  border: "1px solid #334155",
  fontSize: 12,
  cursor: "pointer",
  transition: "all 0.15s",
};

export default function TopAppsChart({ apiBaseUrl, token, topN = 6 }) {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [chartType, setChartType] = useState("vertical");
  const intervalRef = useRef(null);

  const fetchData = async () => {
    try {
      // Cargamos todas las actividades con sus aplicaciones incluidas
      const res = await fetch(`${apiBaseUrl}/api/appactivity`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error("Error al cargar las actividades");
      const activities = await res.json();

      // Agrupamos por display_name y contamos apariciones
      const counts = {};
      for (const act of activities) {
        const name =
          act.application?.display_name ||
          act.application?.process_name ||
          "Desconocida";
        counts[name] = (counts[name] || 0) + 1;
      }

      const sorted = Object.entries(counts)
        .map(([name, value]) => ({
          // Truncamos nombres largos para que quepan en los ejes
          name: name.length > 16 ? name.slice(0, 14) + "…" : name,
          value,
        }))
        .sort((a, b) => b.value - a.value)
        .slice(0, topN);

      setData(sorted);
      setError(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    intervalRef.current = setInterval(fetchData, 30000);
    return () => clearInterval(intervalRef.current);
  }, [apiBaseUrl, token, topN]);

  const activeChart = {
    vertical: <VerticalBars data={data} />,
    horizontal: <HorizontalBars data={data} />,
    donut: <DonutChart data={data} />,
  };

  return (
    <div>
      {/* Selector de tipo de gráfico */}
      <div style={{ display: "flex", gap: 6, marginBottom: 12, flexWrap: "wrap" }}>
        {CHART_TYPES.map((ct) => (
          <button
            key={ct.id}
            onClick={() => setChartType(ct.id)}
            style={{
              ...btnBase,
              background: chartType === ct.id ? "#4ade80" : "transparent",
              color: chartType === ct.id ? "#0f172a" : "#94a3b8",
              borderColor: chartType === ct.id ? "#4ade80" : "#334155",
              fontWeight: chartType === ct.id ? 600 : 400,
            }}
          >
            {ct.label}
          </button>
        ))}
      </div>

      {loading && (
        <p style={{ color: "#94a3b8", textAlign: "center", paddingTop: 40 }}>Cargando...</p>
      )}
      {error && (
        <p style={{ color: "#f87171", textAlign: "center", paddingTop: 40 }}>Error: {error}</p>
      )}
      {!loading && !error && data.length === 0 && (
        <p style={{ color: "#94a3b8", textAlign: "center", paddingTop: 40 }}>
          Sin actividad registrada
        </p>
      )}
      {!loading && !error && data.length > 0 && activeChart[chartType]}
    </div>
  );
}
