const { contextBridge, ipcRenderer } = require('electron/renderer')

contextBridge.exposeInMainWorld('versions', {
  node: () => process.versions.node,
  chrome: () => process.versions.chrome,
  electron: () => process.versions.electron
});

contextBridge.exposeInMainWorld(
  "ipcRenderer",
  {
    sendSync: (channel, ...args) => {
      return ipcRenderer.sendSync(channel, ...args);
    },
    invoke: (channel, ...args) => {
      return ipcRenderer.invoke(channel, ...args);
    }
  }
);