import { IpcRenderer } from 'electron';
import { IpcRequest } from "../shared/IpcRequest";
import { apiNamePrefix } from '../electron/IPC/BaseApiChannel';

export class IpcService {
    private ipcRenderer?: IpcRenderer;

    public send<T>(channel: string, request: IpcRequest = {}): Promise<T> {
        // If the ipcRenderer is not available try to initialize it
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        // If there's no responseChannel let's auto-generate it
        if (!request.responseChannel) {
            request.responseChannel = `${channel}_response_${new Date().getTime()}`
        }

        const ipcRenderer = this.ipcRenderer;
        ipcRenderer.send(channel, request);

        // This method returns a promise which will be resolved when the response has arrived.
        return new Promise(resolve => {
            ipcRenderer.once(request.responseChannel, (event, response) => resolve(response));
        });
    }

    private initializeIpcRenderer() {
        if (!window || !window.process || !window.require) {
            throw new Error(`Unable to require renderer process`);
        }
        this.ipcRenderer = window.require('electron').ipcRenderer;
    }

    public sendApi(apiName: string, request: IpcRequest = { params: {} }) {
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        const channelName = `${apiNamePrefix}${apiName}`;
        if (!request.responseChannel) {
            request.responseChannel = channelName;
        }
        const ipcRenderer = this.ipcRenderer;
        ipcRenderer.send(channelName, request);
        return new Promise(resolve => {
            ipcRenderer.once(channelName, (event, response) => resolve(response));
        });
    }
}