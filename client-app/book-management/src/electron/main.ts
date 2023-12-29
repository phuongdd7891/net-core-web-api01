import { app, BrowserView, BrowserWindow, dialog, ipcMain, Menu, MenuItem, net, session } from 'electron';
import { IpcChannelInterface } from "./IPC/IpcChannelInterface";
import { GeneralInfoChannel } from "./IPC/GeneralInfoChannel";
import { LoginApiChannel, LogoutApiChannel } from './IPC/Api/Login';
import { apiHost, apiNamePrefix, appSessionKey } from './IPC/BaseApiChannel';
import { BookApiChannel } from './IPC/Api/Book';
import * as path from 'node:path';
import { channels } from '../utils';

class Main {
    private mainWindow: BrowserWindow;
    private mainView: BrowserView;
    private loadingView: BrowserView;
    private loadingWindow: BrowserWindow;

    public init(ipcChannels: IpcChannelInterface[]) {
        if (require('electron-squirrel-startup')) app.quit();

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

        ipcMain.once(channels.appExit, () => app.quit())
        ipcMain.on(channels.loaderShow, (event, show) => this.handleLoader(show))

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
            resizable: false
        });
        this.mainView = new BrowserView({
            webPreferences: {
                nodeIntegration: true,
                contextIsolation: false,
                preload: path.join(__dirname, '../preload.js')
            }
        });
        this.mainWindow.addBrowserView(this.mainView);
        this.mainView.setBounds({ x: 0, y: 0, width: 800, height: 600 });
        this.mainView.webContents.openDevTools();
        this.mainView.webContents.loadFile('../../index.html');

        this.loadingWindow = new BrowserWindow({
            parent: this.mainWindow,
            modal: true,
            transparent: true,
            titleBarStyle: 'hidden',
            movable: false,
            show: false
        });
        this.loadingView = new BrowserView();
        this.loadingView.setBounds({ x: 350, y: 250, height: 45, width: 45 });
        this.loadingView.webContents.loadFile('../app/pages/loading.html');
        this.loadingWindow.addBrowserView(this.loadingView);
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
            } else if (channelName == channels.openFile) {
                this.mainView.webContents.loadFile(request.params?.['path']);
            } else if (channelName == channels.dialog) {
                if (request.params?.['type'] == 'err') {
                    dialog.showErrorBox(request.params?.['title'], request.params?.['message']);
                }
            } else if (channelName == channels.message) {
                if (request.params?.['type'] == 'logout') {
                    if (request.params?.['data']) {
                        app.quit();
                    } else {
                        Menu.setApplicationMenu(null);
                        this.mainView.webContents.loadFile('../../index.html');
                    }
                }
            } else {
                if (channelName == channels.menu) {
                    if (request.params?.['type'] == 'user') {
                        const userMenu = new Menu();
                        userMenu.append(new MenuItem({
                            label: `Login: ${sessionData.username}`,
                            submenu: [{
                                label: 'Logout',
                                click: () => this.mainView.webContents.send(channels.menuLogout)
                            }, {
                                label: 'Logout and Quit',
                                click: () => this.mainView.webContents.send(channels.menuLogout, true)
                            }, {
                                type: 'separator'
                            }, {
                                label: 'Quit',
                                click: () => {
                                    app.quit();
                                }
                            }]
                        }));
                        userMenu.append(new MenuItem({
                            label: '|   Book',
                            submenu: [{
                                label: 'List',
                                click: () => this.mainView.webContents.loadFile('../app/pages/book/book.html')
                            }]
                        }));
                        Menu.setApplicationMenu(userMenu);
                    }
                }
                channel.handle(event, request);
            }
        }));
    }

    private handleLoader(show: boolean) {
        if (show) {
            this.loadingWindow.show();
            this.mainView.webContents.executeJavaScript("window.$ = require(\"jquery\");$('<div class=\"modal-backdrop\" style=\"opacity:0.3 !important;\"></div>').appendTo(document.body);0");
        } else {
            this.loadingWindow.hide();
            this.mainView.webContents.executeJavaScript("window.$ = require(\"jquery\");$(\".modal-backdrop\").remove();0");
        }
    }
}

(new Main()).init([
    new GeneralInfoChannel("wd"),
    new GeneralInfoChannel("dialog"),
    new GeneralInfoChannel("menu"),
    new GeneralInfoChannel("msg"),
    new LoginApiChannel(),
    new LogoutApiChannel(),
    new BookApiChannel()
]);