import { IpcRenderer } from 'electron';
import { IpcRequest } from "../shared/IpcRequest";
import { apiNamePrefix } from '../electron/IPC/BaseApiChannel';
import { channels } from '../utils';

export class IpcService {
    private ipcRenderer?: IpcRenderer;

    public send<T>(channel: string, request: IpcRequest = { params: {} }): Promise<T> {
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

    public async sendApi<T>(apiName: string, request: IpcRequest = { params: {} }): Promise<T> {
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        const channelName = `${apiNamePrefix}${apiName}`;
        if (!request.responseChannel) {
            request.responseChannel = channelName;
        }
        const ipcRenderer = this.ipcRenderer;
        ipcRenderer.send(channelName, request);
        ipcRenderer.send(channels.loaderShow, true);
        return new Promise<T>(resolve => {
            ipcRenderer.once(channelName, async (event, response) => resolve(response));
        }).finally(async () => {
            ipcRenderer.send(channels.loaderShow, false)
        });
    }

    public sendDialogError(title: string, message: string) {
        this.ipcRenderer.send(channels.dialog, {
            params: {
                message: message,
                title: title,
                type: 'err'
            }
        })
    }
}