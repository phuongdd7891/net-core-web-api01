import { IpcRenderer } from 'electron';
import { IpcRequest, IpcResponse } from "../shared/IpcRequest";
import { apiHost, apiNamePrefix } from '../electron/IPC/BaseApiChannel';
import { channels, storeKeys } from '../utils';

export class IpcService {
    private ipcRenderer?: IpcRenderer;
    private readonly pageScripts = {
        './book/book.html': './book/book.js',
        './book/create.html': './book/create.js',
        './book-category/category.html': './book-category/category.js',
        './book-category/create.html': './book-category/create.js'
    }
    public readonly pagePaths = {
        book: './book/book.html',
        createBook: './book/create.html',
        category: './book-category/category.html',
        createCategory: './book-category/create.html'
    }

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
            ipcRenderer.once(request.responseChannel!, (event, response) => resolve(response));
        });
    }

    private initializeIpcRenderer() {
        if (!window || !window.process || !window.require) {
            throw new Error(`Unable to require renderer process`);
        }
        this.ipcRenderer = window.require('electron').ipcRenderer;
    }

    public sendApi<T extends IpcResponse>(apiName: string, request: IpcRequest = { params: {} }): Promise<T> {
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
            ipcRenderer.once(channelName, (event, response) => resolve(response));
        }).then(res => ({...res, success: res.code == 200})).finally(() => {
            ipcRenderer.send(channels.loaderShow, false)
        });
    }

    public sendDialogError(message: any, title: string = '') {
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        this.ipcRenderer.send(channels.dialog, {
            params: {
                message: !message ? 'Unknown error' : typeof message == "string" ? message : (message.message || JSON.stringify(message)),
                title: title,
                type: "error"
            }
        })
    }

    public sendDialogInfo(message: any, title: string = '', type: string = 'info', buttons: string[] = []) {
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        this.ipcRenderer.send(channels.dialog, {
            params: {
                message,
                title,
                type,
                buttons
            }
        });
        const ipcRenderer = this.ipcRenderer;
        return new Promise<number>(resolve => {
            ipcRenderer.once(channels.dialog, (event, response) => resolve(response))
        })
    }

    public sendOpenFile(filePath: string, scriptPath?: string, queryParams?: any) {
        if (filePath) {
            if (!scriptPath) {
                scriptPath = this.pageScripts[filePath];
            }
            this.send(channels.openFile, {
                params: {
                    query: {
                        ...queryParams,
                        path: filePath,
                        script: scriptPath
                    }
                }
            })
        }
    }

    public async getImageSrc(id: string) {
        var userStore = await this.getAppStore(storeKeys.userStore);
        let src: string = '';
        if (userStore) {
            src = `${apiHost}/api/books/download-cover?id=${id}&u=${userStore.username}`
        }
        return src;
    }

    private setAppStore(channel: string, data: any) {
        this.ipcRenderer!.send(channel, data);
    }

    private getAppStore(channel: string, arg?: any) {
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        return this.ipcRenderer!.invoke(channel, arg);
    }

    public showLoader(show: boolean) {
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        this.ipcRenderer.send(channels.loaderShow, show);
    }

    public getRenderer() {
        if (!this.ipcRenderer) {
            this.initializeIpcRenderer();
        }
        return this.ipcRenderer;
    }

    public setUserNotifications(message: string) {
        this.setAppStore(channels.setUserNotifications, message);
    }

    public getUserNotifications() {
        return this.getAppStore(channels.userNotifications);
    }

    public setUserStore(data: any) {
        this.setAppStore(channels.setUserStore, data);
    }

    public getUserStore(username?: string) {
        return this.getAppStore(channels.userStore, username);
    }
}