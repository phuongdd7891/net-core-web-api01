import { app, BrowserWindow, ipcMain, net, protocol, session } from 'electron';
import { IpcChannelInterface } from "./IPC/IpcChannelInterface";
import { SystemInfoChannel } from "./IPC/SystemInfoChannel";
import { LoginApiChannel } from './IPC/Api/Login';
import { apiHost, apiNamePrefix, appSessionKey } from './IPC/BaseApiChannel';
import { BookApiChannel } from './IPC/Api/Book';

class Main {
    private mainWindow: BrowserWindow;

    public init(ipcChannels: IpcChannelInterface[]) {
        app.on('ready', () => {
            this.createWindow();

            session.defaultSession.webRequest.onBeforeSendHeaders({
                urls: [`${apiHost}/api/*`]
            }, async (details, callback) => {
                const [cookies] = await session.defaultSession.cookies.get({ url: 'http://localhost/', name: appSessionKey });
                if (cookies) {
                    const sessionData = JSON.parse(cookies.value);
                    details.requestHeaders['ApiKey'] = sessionData.token;
                }
                callback({
                    requestHeaders: details.requestHeaders
                });
            })
        })

        app.on('window-all-closed', this.onWindowAllClosed);
        app.on('activate', this.onActivate);

        this.registerIpcChannels(ipcChannels);
    }

    private async onWindowAllClosed() {
        await session.defaultSession.cookies.remove('http://localhost/', appSessionKey);
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
        ipcChannels.forEach(channel => ipcMain.on(channel.getName(), async (event, request) => {
            if (channel.getName().startsWith(apiNamePrefix)) {
                const [cookies] = await session.defaultSession.cookies.get({ url: 'http://localhost/', name: appSessionKey });
                if (cookies) {
                    const sessionData = JSON.parse(cookies.value);
                    request.params["username"] = sessionData.username;
                }
                channel.handleNet(event, request, net);
            } else {
                channel.handle(event, request);
            }
        }));
    }
}

(new Main()).init([
    new SystemInfoChannel(),
    new LoginApiChannel(),
    new BookApiChannel()
]);