import { contextBridge, ipcRenderer } from 'electron/renderer';
import { IpcService } from './app/IpcService';
import { IpcResponse } from './shared/IpcRequest';

const ipcService = new IpcService();

window.addEventListener("DOMContentLoaded", () => {
    ipcRenderer.on('menu-logout', async (_event, value) => {
        const response = await ipcService.sendApi<IpcResponse>('logout');
        if (response.code == 200) {
            ipcService.send("msg", {
                params: {
                    type: 'logout'
                }
            });
        }
    })

    const replaceText = (selector: string, text: string) => {
        const element = document.getElementById(selector);
        if (element) {
            element.innerText = text;
        }
    };

    for (const type of ["chrome", "node", "electron"]) {
        replaceText(`${type}-version`, process.versions[type as keyof NodeJS.ProcessVersions]);
    }
});