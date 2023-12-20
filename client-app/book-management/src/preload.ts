import { contextBridge, ipcRenderer } from 'electron/renderer';

contextBridge.exposeInMainWorld('versions', {
    node: () => process.versions.node,
    chrome: () => process.versions.chrome,
    electron: () => process.versions.electron
});

contextBridge.exposeInMainWorld(
    "ipcRenderer",
    {
        sendSync: (channel: string, ...args: any[]) => {
            return ipcRenderer.sendSync(channel, ...args);
        },
        invoke: (channel: string, ...args: any[]) => {
            return ipcRenderer.invoke(channel, ...args);
        }
    }
);