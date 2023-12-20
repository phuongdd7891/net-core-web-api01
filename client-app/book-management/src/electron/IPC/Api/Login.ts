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
            await session.defaultSession.cookies.set({
                path: '/',
                domain: 'localhost',
                url: 'http://localhost/',
                name: appSessionKey,
                value: JSON.stringify({
                    username: response.Username,
                    token: response.Value
                })
            })
            event.reply(request.responseChannel, response);
        })
    }

}