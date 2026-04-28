import { useEffect, useState, useRef } from "react";
import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";

const COLORS = {
  active: "#4ade80",
  inactive: "#64748b",
};

const RADIAN = Math.PI / 180;
const renderCustomLabel = ({ cx, cy, midAngle, innerRadius, outerRadius, percent }) => {
  const radius = innerRadius + (outerRadius - innerRadius) * 0.5;
  const x = cx + radius * Math.cos(-midAngle * RADIAN);
  const y = cy + radius * Math.sin(-midAngle * RADIAN);
  return percent > 0.05 ? (
    <text x={x} y={y} fill="white" textAnchor="middle" dominantBaseline="central" fontSize={13} fontWeight={500}>
      {`${(percent * 100).toFixed(0)}%`}
    </text>
  ) : null;
};

export default function ActivityPieChart({ apiBaseUrl, token }) {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const intervalRef = useRef(null);

  const fetchData = async () => {
    try {
      // Obtenemos los periodos de actividad activos
      const res = await fetch(`${apiBaseUrl}/api/activityperiods`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error("Error al cargar los periodos");
      const periods = await res.json();

      // Contamos periodos abiertos (sin period_end) por status
      const open = periods.filter((p) => !p.period_end);
      const active = open.filter((p) => p.status === "active").length;
      const inactive = open.filter((p) => p.status !== "active").length;

      setData([
        { name: "Activos", value: active },
        { name: "Inactivos", value: inactive },
      ]);
      setError(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    // Refresca cada 30 segundos
    intervalRef.current = setInterval(fetchData, 30000);
    return () => clearInterval(intervalRef.current);
  }, [apiBaseUrl, token]);

  if (loading) return <p style={{ color: "#94a3b8", textAlign: "center", paddingTop: 40 }}>Cargando...</p>;
  if (error) return <p style={{ color: "#f87171", textAlign: "center", paddingTop: 40 }}>Error: {error}</p>;
  if (data.every((d) => d.value === 0)) {
    return <p style={{ color: "#94a3b8", textAlign: "center", paddingTop: 40 }}>Sin sesiones activas</p>;
  }

  return (
    <ResponsiveContainer width="100%" height={220}>
      <PieChart>
        <Pie
          data={data}
          cx="50%"
          cy="50%"
          outerRadius={80}
          dataKey="value"
          labelLine={false}
          label={renderCustomLabel}
        >
          {data.map((entry) => (
            <Cell
              key={entry.name}
              fill={entry.name === "Activos" ? COLORS.active : COLORS.inactive}
            />
          ))}
        </Pie>
        <Tooltip
          contentStyle={{ background: "#1e293b", border: "none", borderRadius: 8, color: "#f1f5f9" }}
          formatter={(value, name) => [`${value} trabajadores`, name]}
        />
        <Legend
          formatter={(value, entry) => (
            <span style={{ color: "#94a3b8", fontSize: 13 }}>
              {value}: <strong style={{ color: "#f1f5f9" }}>{entry.payload.value}</strong>
            </span>
          )}
        />
      </PieChart>
    </ResponsiveContainer>
  );
}
