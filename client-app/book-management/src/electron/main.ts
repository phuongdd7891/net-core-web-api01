import { app, BrowserWindow, ipcMain, net } from 'electron';
import { IpcChannelInterface } from "./IPC/IpcChannelInterface";
import { SystemInfoChannel } from "./IPC/SystemInfoChannel";
import { LoginApiChannel } from './IPC/Api/Login';
import { apiNamePrefix } from './IPC/BaseApiChannel';

class Main {
    private mainWindow: BrowserWindow;

    public init(ipcChannels: IpcChannelInterface[]) {
        app.on('ready', this.createWindow);
        app.on('window-all-closed', this.onWindowAllClosed);
        app.on('activate', this.onActivate);

        this.registerIpcChannels(ipcChannels);
    }

    private onWindowAllClosed() {
        if (process.platform !== 'darwin') {
            app.quit();
        }
    }

    private onActivate() {
        if (!this.mainWindow) {
            this.createWindow();
        }
    }

    private createWindow() {
        this.mainWindow = new BrowserWindow({
            height: 600,
            width: 800,
            title: `Yet another Electron Application`,
            webPreferences: {
                nodeIntegration: true,
                contextIsolation: false,
            }
        });

        this.mainWindow.webContents.openDevTools();
        this.mainWindow.loadFile('../../index.html');
    }

    private registerIpcChannels(ipcChannels: IpcChannelInterface[]) {
        ipcChannels.forEach(channel => ipcMain.on(channel.getName(), (event, request) => {
            if (channel.getName().startsWith(apiNamePrefix)) {
                channel.handleNet(event, request, net);
            } else {
                channel.handle(event, request);
            }
        }));
    }
}

(new Main()).init([
    new SystemInfoChannel(),
    new LoginApiChannel()
]);