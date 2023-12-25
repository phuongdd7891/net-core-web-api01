import { app, BrowserWindow, dialog, ipcMain, Menu, MenuItem, net, session } from 'electron';
import { IpcChannelInterface } from "./IPC/IpcChannelInterface";
import { GeneralInfoChannel } from "./IPC/GeneralInfoChannel";
import { LoginApiChannel, LogoutApiChannel } from './IPC/Api/Login';
import { apiHost, apiNamePrefix, appSessionKey } from './IPC/BaseApiChannel';
import { BookApiChannel } from './IPC/Api/Book';
import * as path from 'node:path';

class Main {
    private mainWindow: BrowserWindow;

    public init(ipcChannels: IpcChannelInterface[]) {
        app.on('ready', () => {
            Menu.setApplicationMenu(null);
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
                preload: path.join(__dirname, '../preload.js')
            }
        });

        this.mainWindow.webContents.openDevTools();
        this.mainWindow.loadFile('../../index.html');
    }

    private registerIpcChannels(ipcChannels: IpcChannelInterface[]) {
        ipcChannels.forEach(channel => ipcMain.on(channel.getName(), async (event, request) => {
            const channelName = channel.getName();
            const [cookies] = await session.defaultSession.cookies.get({ url: 'http://localhost/', name: appSessionKey });
            const sessionData = cookies ? JSON.parse(cookies.value) : null;
            if (sessionData) {
                request.params["username"] = sessionData.username;
            }
            if (channelName.startsWith(apiNamePrefix)) {
                channel.handleNet(event, request, net);
            } else if (channelName == 'wd') {
                this.mainWindow.loadFile(request.params?.['path']);
            } else if (channelName == 'dialog') {
                if (request.params?.['type'] == 'err') {
                    dialog.showErrorBox(request.params?.['title'], request.params?.['message']);
                }
            } else if (channelName == 'msg') {
                if (request.params?.['type'] == 'logout') {
                    Menu.setApplicationMenu(null);
                    this.mainWindow.loadFile('../../index.html');
                }
            } else {
                if (channelName == 'menu') {
                    if (request.params?.['type'] == 'user') {
                        const userMenu = new Menu();
                        userMenu.append(new MenuItem({
                            label: sessionData.username,
                            submenu: [{
                                label: 'Logout',
                                click: () => this.mainWindow.webContents.send("menu-logout")
                            }]
                        }));
                        Menu.setApplicationMenu(userMenu);
                    }
                }
                channel.handle(event, request);
            }
        }));
    }
}

(new Main()).init([
    new GeneralInfoChannel(),
    new GeneralInfoChannel("wd"),
    new GeneralInfoChannel("dialog"),
    new GeneralInfoChannel("menu"),
    new GeneralInfoChannel("msg"),
    new LoginApiChannel(),
    new LogoutApiChannel(),
    new BookApiChannel()
]);