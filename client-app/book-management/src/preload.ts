import { contextBridge, ipcRenderer } from 'electron/renderer';
import { IpcService } from './app/IpcService';
import { IpcResponse } from './shared/IpcRequest';
import { channels } from './utils';

const ipcService = new IpcService();

window.addEventListener("DOMContentLoaded", () => {
    ipcRenderer.on(channels.menuLogout, async (_event, value) => {
        const response = await ipcService.sendApi<IpcResponse>('logout');
        if (response.code == 200) {
            ipcService.send(channels.message, {
                params: {
                    type: 'logout',
                    data: value
                }
            });
        } else {
            ipcService.sendDialogError(response.data)
        }
    })

    const replaceText = (selector: string, text: string) => {
        const element = document.getElementById(selector);
        if (element) {
            element.innerText = text;
        }
    };

    for (const type of ["chrome", "node", "electron"]) {
        replaceText(`${type}-version`, process.versions[type as keyof NodeJS.ProcessVersions]!);
    }
});