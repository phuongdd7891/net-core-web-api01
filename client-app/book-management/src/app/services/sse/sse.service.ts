import { apiHost } from "../../../electron/IPC/BaseApiChannel";

export class SseService {
    private es: EventSource | null = null;
    public connect(username: string) {
        this.es = new EventSource(`${apiHost}/sse/connect?u=${username}`);
        this.es.onerror = ev => {
            console.error(ev);
        };
        return this.es;
    }

    public close() {
        this.es?.close();
    }
}