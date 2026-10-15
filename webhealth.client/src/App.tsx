import { useEffect, useState } from 'react';
import { HubConnectionBuilder, HubConnection, HubConnectionState } from '@microsoft/signalr';
import './App.css';

interface PhaseResult {
    phaseName: string;
    isSuccess: boolean;
    durationMs: number;
    errorMessage?: string | null;
}

interface HostUpdate {
    hostId: string;
    name: string;
    status: 'Healthy' | 'Degraded' | 'Down' | 'Unknown';
    latencyMs: number;
    averageLatencyMs: number;
    uptime: number;
    phases: PhaseResult[];
    timestamp: string;
}

interface HostTarget {
    id: string;
    name: string;
    url: string;
    isMock: boolean;
    isActive: boolean;
    intervalSeconds: number;
}

function App() {
    const [hosts, setHosts] = useState<HostTarget[]>([]);
    const [liveUpdates, setLiveUpdates] = useState<Record<string, HostUpdate>>({});
    const [logs, setLogs] = useState<string[]>([]);
    const [connected, setConnected] = useState<boolean>(false);

    // Form state
    const [name, setName] = useState('');
    const [url, setUrl] = useState('');
    const [isMock, setIsMock] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [urlError, setUrlError] = useState<string | null>(null);

    const isValidUrl = (urlString: string): boolean => {
        try {
            const parsed = new URL(urlString);
            return parsed.protocol === 'http:' || parsed.protocol === 'https:';
        } catch {
            return false;
        }
    };

    const fetchHosts = async () => {
        try {
            const res = await fetch('/api/hosts');
            if (res.ok) {
                const data: HostTarget[] = await res.json();
                setHosts(data);
            }
        } catch (err) {
            console.error('Failed to load hosts:', err);
        }
    };

    useEffect(() => {
        const loadInitialHosts = async () => {
            try {
                const res = await fetch('/api/hosts');
                if (res.ok) {
                    const data: HostTarget[] = await res.json();
                    setHosts(data);
                }
            } catch (err) {
                console.error('Failed to load hosts:', err);
            }
        };
        loadInitialHosts();

        const connection: HubConnection = new HubConnectionBuilder()
            .withUrl('/healthHub')
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveHealthUpdate', (update: HostUpdate) => {
            setLiveUpdates(prev => ({
                ...prev,
                [update.hostId]: update
            }));

            const timeStr = new Date().toLocaleTimeString();
            const logEntry = `[${timeStr}] ${update.name} ➔ ${update.status} (${update.latencyMs}ms)`;
            setLogs(prev => [logEntry, ...prev.slice(0, 19)]);
        });

        connection.start()
            .then(() => setConnected(true))
            .catch(err => {
                console.error('SignalR Connection Error: ', err);
                setConnected(false);
            });

        connection.onreconnecting(() => setConnected(false));
        connection.onreconnected(() => setConnected(true));
        connection.onclose(() => setConnected(false));

        return () => {
            if (connection.state === HubConnectionState.Connected) {
                connection.stop();
            }
        };
    }, []);

    const handleAddHost = async (e: React.FormEvent) => {
        e.preventDefault();
        setUrlError(null);
        if (!name.trim() || !url.trim()) return;

        const trimmedUrl = url.trim();
        if (!isValidUrl(trimmedUrl)) {
            setUrlError('Please enter a valid website URL (must start with http:// or https://)');
            return;
        }

        setIsSubmitting(true);
        try {
            const res = await fetch('/api/hosts', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    name: name.trim(),
                    url: trimmedUrl,
                    isMock: isMock,
                    isActive: true,
                    intervalSeconds: 5
                })
            });

            if (res.ok) {
                setName('');
                setUrl('');
                setIsMock(false);
                await fetchHosts();
            }
        } catch (err) {
            console.error('Failed to add host:', err);
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleDeleteHost = async (id: string) => {
        try {
            const res = await fetch(`/api/hosts/${id}`, { method: 'DELETE' });
            if (res.ok) {
                setHosts(prev => prev.filter(h => h.id !== id));
                setLiveUpdates(prev => {
                    const next = { ...prev };
                    delete next[id];
                    return next;
                });
            }
        } catch (err) {
            console.error('Failed to delete host:', err);
        }
    };

    return (
        <div className="dashboard-container">
            {/* Header */}
            <header className="dashboard-header">
                <div className="brand">
                    <span className="pulse-icon">⚡</span>
                    <h1>WebHealth Pulse</h1>
                    <span className="version-badge">v1.0 Distributed</span>
                </div>
                <div className={`connection-pill ${connected ? 'connected' : 'disconnected'}`}>
                    <span className="indicator-dot"></span>
                    {connected ? 'SignalR Live' : 'Connecting...'}
                </div>
            </header>

            {/* Add Host Form */}
            <section className="card add-host-card">
                <h2>Add Target to Monitor</h2>
                <form onSubmit={handleAddHost} className="host-form">
                    <input
                        type="text"
                        placeholder="Service Name (e.g. Google Search)"
                        value={name}
                        onChange={e => setName(e.target.value)}
                        required
                    />
                    <input
                        type="text"
                        placeholder="URL (e.g. https://google.com or https://mock/500)"
                        value={url}
                        onChange={e => {
                            setUrl(e.target.value);
                            if (urlError) setUrlError(null);
                        }}
                        required
                    />
                    <label className="checkbox-label">
                        <input
                            type="checkbox"
                            checked={isMock}
                            onChange={e => setIsMock(e.target.checked)}
                        />
                        <span>Mock Mode</span>
                    </label>
                    <button type="submit" disabled={isSubmitting} className="btn-primary">
                        {isSubmitting ? 'Adding...' : '+ Add Host'}
                    </button>
                </form>
                {urlError && <div className="form-error-msg">⚠️ {urlError}</div>}
            </section>

            {/* Monitored Hosts Grid */}
            <section className="hosts-section">
                <h2>Monitored Services ({hosts.length})</h2>
                <div className="hosts-grid">
                    {hosts.map(host => {
                        const update = liveUpdates[host.id];
                        const status = update?.status || 'Unknown';
                        const statusClass = status.toLowerCase();

                        return (
                            <div key={host.id} className={`card host-card status-border-${statusClass}`}>
                                <div className="host-card-header">
                                    <div>
                                        <div className="host-title-row">
                                            <h3>{host.name}</h3>
                                            {host.isMock && <span className="mock-tag">MOCK</span>}
                                        </div>
                                        <a href={host.url} target="_blank" rel="noreferrer" className="host-url">
                                            {host.url}
                                        </a>
                                    </div>
                                    <span className={`status-badge status-${statusClass}`}>
                                        {status}
                                    </span>
                                </div>

                                <div className="metrics-row">
                                    <div className="metric-box">
                                        <span className="metric-label">Latest Latency</span>
                                        <span className="metric-value">
                                            {update ? `${update.latencyMs} ms` : '—'}
                                        </span>
                                    </div>
                                    <div className="metric-box">
                                        <span className="metric-label">Average (50x)</span>
                                        <span className="metric-value">
                                            {update ? `${update.averageLatencyMs.toFixed(0)} ms` : '—'}
                                        </span>
                                    </div>
                                    <div className="metric-box">
                                        <span className="metric-label">Uptime</span>
                                        <span className="metric-value highlight">
                                            {update ? `${update.uptime.toFixed(1)}%` : '—'}
                                        </span>
                                    </div>
                                </div>

                                {update && update.phases && (
                                    <div className="phases-container">
                                        <span className="phases-title">Parallel Barrier Phases:</span>
                                        <div className="phase-chips">
                                            {update.phases.map((phase, idx) => (
                                                <div
                                                    key={idx}
                                                    className={`phase-chip ${phase.isSuccess ? 'phase-ok' : 'phase-fail'}`}
                                                    title={phase.errorMessage || 'Success'}
                                                >
                                                    <span className="phase-name">{phase.phaseName}</span>
                                                    <span className="phase-duration">{phase.durationMs}ms</span>
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                )}

                                <div className="host-card-footer">
                                    <span className="timestamp">
                                        {update ? `Updated: ${new Date(update.timestamp).toLocaleTimeString()}` : 'Waiting for ping...'}
                                    </span>
                                    <button
                                        onClick={() => handleDeleteHost(host.id)}
                                        className="btn-danger"
                                        title="Delete host"
                                    >
                                        Delete
                                    </button>
                                </div>
                            </div>
                        );
                    })}
                </div>
            </section>

            {/* Live Terminal Log */}
            <section className="card terminal-section">
                <div className="terminal-header">
                    <span>🔴 🟡 🟢</span>
                    <span className="terminal-title">Real-Time SignalR Event Stream</span>
                </div>
                <div className="terminal-body">
                    {logs.length === 0 ? (
                        <p className="terminal-empty">Listening for health check broadcasts from .NET BackgroundService...</p>
                    ) : (
                        logs.map((log, i) => (
                            <div key={i} className="log-line">
                                {log}
                            </div>
                        ))
                    )}
                </div>
            </section>
        </div>
    );
}

export default App;