import { Net, session } from "electron";
import { IpcRequest } from "src/shared/IpcRequest";
import { BaseApiChannel, appSessionKey } from "../BaseApiChannel";
import { NetUtils } from "../../../utils";

export class LoginApiChannel extends BaseApiChannel {

    constructor() {
        super("login");
    }

    handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: Net): void {
        NetUtils.postRequest('api/Operations/login', request, net).then(async (response: any) => {
            if (response.code == 200) {
                await session.defaultSession.cookies.set({
                    path: '/',
                    domain: 'localhost',
                    url: 'http://localhost/',
                    name: appSessionKey,
                    value: JSON.stringify({
                        username: response.data.username,
                        token: response.data.value
                    })
                })
            }
            event.reply(request.responseChannel, response);
        }).catch(err => {
            event.reply(request.responseChannel, {
                code: 500,
                data: err
            });
        })
    }

}

export class LogoutApiChannel extends BaseApiChannel {

    constructor() {
        super("logout");
    }

    handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: Net): void {
        NetUtils.postRequest('api/Operations/logout', request, net).then(async (response: any) => {
            if (response.code == 200) {
                await session.defaultSession.cookies.remove('http://localhost/', appSessionKey);
            }
            event.reply(request.responseChannel, response);
        }).catch(err => {
            event.reply(request.responseChannel, {
                code: 500,
                data: err
            });
        })
    }

}