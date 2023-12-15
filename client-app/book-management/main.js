const { app, BrowserWindow } = require('electron/main')
const { globalShortcut, ipcMain, net, session } = require('electron')
const path = require('node:path')
const os = require('os')

const apiHost = 'http://localhost:5253'
const bookSessionKey = '__book_session';

const createWindow = () => {
    let mainWindow = new BrowserWindow({
        width: 800,
        height: 600,
        webPreferences: {
            nodeIntegration: true,
            defaultEncoding: 'UTF-8',
            worldSafeExecuteJavaScript: true,
            /* See https://stackoverflow.com/questions/63427191/security-warning-in-the-console-of-browserwindow-electron-9-2-0 */
            enableRemoteModule: true,
            preload: path.join(__dirname, 'preload.js')
        }
    })

    mainWindow.loadURL(path.join(__dirname, 'login/login.html'))

    // Enable keyboard shortcuts for Developer Tools on various platforms.
    let platform = os.platform()
    if (platform === 'darwin') {
        globalShortcut.register('Command+Option+I', () => {
            mainWindow.webContents.openDevTools()
        })
    } else if (platform === 'linux' || platform === 'win32') {
        //globalShortcut.register('Control+Shift+I', () => {
        mainWindow.webContents.openDevTools()
        //})
    }

    mainWindow.once('ready-to-show', () => {
        mainWindow.setMenu(null)
        mainWindow.show()
    })

    mainWindow.onbeforeunload = (e) => {
        // Prevent Command-R from unloading the window contents.
        //e.returnValue = false
    }

    mainWindow.on('closed', function () {
        mainWindow = null
    })
}

app.whenReady().then(() => {
    createWindow()

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) {
            createWindow()
        }
    })

    session.defaultSession.webRequest.onBeforeSendHeaders({
        urls: [`${apiHost}/api/*`]
    }, async (details, callback) => {
        var [cookies] = await session.defaultSession.cookies.get({ url: 'http://localhost/', name: bookSessionKey });
        if (cookies) {
            var sessionData = JSON.parse(cookies.value);
            details.requestHeaders['ApiKey'] = sessionData.token;
        }
        callback({
            requestHeaders: details.requestHeaders
        });
    })

    ipcMain.on('entry-accepted', (event, arg) => {
        console.log(arg)
        event.returnValue = 'pong'
    })

    ipcMain.handle('login', async (event, ...args) => {
        var [username, password] = args;
        let buffers = [];

        return new Promise((resolve, reject) => {
            const request = net.request({
                url: `${apiHost}/api/Operations/login`,
                method: 'post',
                headers: {
                    "Content-Type": "application/json"
                }
            })
            request.write(JSON.stringify({
                "UserName": username,
                "Password": password
            }))
            request.on('response', (response) => {
                response.on('data', (chunk) => {
                    if (response.statusCode != 200) {
                        console.log(`BODY: ${chunk}`)
                    }
                    buffers.push(chunk);
                })
                response.on('error', (error) => {
                    console.log(`ERROR: ${JSON.stringify(error)}`)
                    reject(error)
                })
                response.on('end', async () => {
                    let responseBodyBuffer = Buffer.concat(buffers);
                    let responseBodyJSON = JSON.parse(responseBodyBuffer.toString());
                    await session.defaultSession.cookies.set({
                        path: '/',
                        domain: 'localhost',
                        url: 'http://localhost/',
                        name: bookSessionKey,
                        value: JSON.stringify({
                            username: responseBodyJSON.Username,
                            token: responseBodyJSON.Value
                        })
                    })
                    resolve(responseBodyJSON);
                })
            })
            request.end()
        })

    })

    ipcMain.handle('books', async (event, ...args) => {
        let buffers = [];

        return new Promise((resolve, reject) => {
            const request = net.request({
                url: `${apiHost}/api/books`,
                headers: {
                    "Content-Type": "application/json"
                }
            })
            request.on('response', (response) => {
                response.on('data', (chunk) => {
                    if (response.statusCode != 200) {
                        console.log(`BODY: ${chunk}`)
                    }
                    buffers.push(chunk);
                })
                response.on('error', (error) => {
                    console.log(`ERROR: ${JSON.stringify(error)}`)
                    reject(error)
                })
                response.on('end', () => {
                    let responseBodyBuffer = Buffer.concat(buffers);
                    let responseBodyJSON = JSON.parse(responseBodyBuffer.toString());
                    resolve(responseBodyJSON);
                })
            })
            request.end()
        })

    })
})

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit()
    }
})