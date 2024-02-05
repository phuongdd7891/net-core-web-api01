import { Net, session } from "electron";
import { IpcRequest } from "../../../shared/IpcRequest";
import { BaseApiChannel } from "../BaseApiChannel";
import { NetUtils } from "../../../utils";
import appData from "../../../electron/main";

export class LoginApiChannel extends BaseApiChannel {

    constructor() {
        super("login");
    }

    handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: Net): void {
        NetUtils.postRequest('api/Operations/login?t=jwt', request, net).then(async (response: any) => {
            if (response.code == 200) {
                const data = {
                    username: response.data.username,
                    token: response.data.token
                };
                await session.defaultSession.cookies.set({
                    path: '/',
                    domain: 'localhost',
                    url: 'http://localhost/',
                    name: `${appData.deviceId}`,
                    value: JSON.stringify(data)
                })
                appData.username = data.username;
            }
            event.reply(request.responseChannel, response);
        }).catch(err => {
            event.reply(request.responseChannel!, {
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
                await session.defaultSession.cookies.remove('http://localhost/', `${appData.deviceId}`);
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

export class ChangePasswordApiChannel extends BaseApiChannel {
    constructor() {
        super("change-password");
    }
}