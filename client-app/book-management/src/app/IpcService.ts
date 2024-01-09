import { IpcRenderer } from 'electron';
import { IpcRequest } from "../shared/IpcRequest";
import { apiHost, apiNamePrefix } from '../electron/IPC/BaseApiChannel';
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

        const ipcRenderer = this.ipcRenderer!;
        ipcRenderer.send(channel, request);

        // This method returns a promise which will be resolved when the response has arrived.
        return new Promise(resolve => {
            ipcRenderer.once(request.responseChannel!, (event, response) => resolve(response));
        });
    }

    private initializeIpcRenderer() {
        if (!window || !window.process || !window.require) {
            throw new Error(`Unable to require renderer process`);
        }
        this.ipcRenderer = window.require('electron').ipcRenderer;
    }

    public sendApi<T>(apiName: string, request: IpcRequest = { params: {} }): Promise<T> {
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        const channelName = `${apiNamePrefix}${apiName}`;
        if (!request.responseChannel) {
            request.responseChannel = channelName;
        }
        const ipcRenderer = this.ipcRenderer!;
        ipcRenderer.send(channelName, request);
        ipcRenderer.send(channels.loaderShow, true);
        return new Promise<T>(resolve => {
            ipcRenderer.once(channelName, (event, response) => resolve(response));
        }).finally(() => {
            ipcRenderer.send(channels.loaderShow, false)
        });
    }

    public sendDialogError(message: any, title: string = '') {
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        this.ipcRenderer.send(channels.dialog, {
            params: {
                message: typeof message == "string" ? message : (message.message || JSON.stringify(message)),
                title: title,
                type: "error"
            }
        })
    }

    public sendDialogInfo(message: any, title: string = '') {
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        this.ipcRenderer.send(channels.dialog, {
            params: {
                message: message,
                title: title,
                type: "info"
            }
        });
        const ipcRenderer = this.ipcRenderer!;
        return new Promise(resolve => {
            ipcRenderer.once(channels.dialog, (event, response) => resolve(response))
        })
    }

    public sendOpenFile(filePath: string, params?: any) {
        if (filePath) {
            this.send(channels.openFile, {
                params: {
                    ...params,
                    path: filePath
                }
            })
        }
    }

    public getAppCookies() {
        const cookies = require('@electron/remote').require('../electron/main').default;
        return cookies;
    }

    public getImageSrc(id: string) {
        const cookies = this.getAppCookies();
        let src: string = '';
        if (cookies) {
            src = `${apiHost}/api/books/download-cover?id=${id}&u=${cookies.data?.username}`
        }
        return src;
    }
}