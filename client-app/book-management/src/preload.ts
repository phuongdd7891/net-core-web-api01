import { contextBridge, ipcRenderer } from 'electron/renderer';
import { IpcService } from './app/IpcService';
import { IpcResponse } from './shared/IpcRequest';

const ipcService = new IpcService();

// contextBridge.exposeInMainWorld('versions', {
//     node: () => process.versions.node,
//     chrome: () => process.versions.chrome,
//     electron: () => process.versions.electron
// });

// contextBridge.exposeInMainWorld(
//     "ipcRenderer",
//     {
//         sendSync: (channel: string, ...args: any[]) => {
//             return ipcRenderer.sendSync(channel, ...args);
//         },
//         invoke: (channel: string, ...args: any[]) => {
//             return ipcRenderer.invoke(channel, ...args);
//         }
//     }
// );

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
});