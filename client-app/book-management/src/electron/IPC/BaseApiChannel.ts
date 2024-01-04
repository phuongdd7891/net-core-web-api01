import { IpcRequest } from "src/shared/IpcRequest";
import { IpcChannelInterface } from "./IpcChannelInterface";
import { NetUtils } from "../../utils";
import { Net } from "electron";

export const apiHost: string = 'http://localhost:5253';
export const apiNamePrefix: string = 'api-';
export const appSessionKey: string = '__app_session';
export const apiEndpointKey: string = 'endpoint';
export const apiMethodKey: string = 'method';

export abstract class BaseApiChannel implements IpcChannelInterface {
    private _apiName: string;

    constructor(apiName: string) {
        this._apiName = apiName;
    }

    getName(): string {
        return `${apiNamePrefix}${this._apiName}`;
    }

    handle(event: Electron.IpcMainEvent, request: IpcRequest): void {
        throw new Error("Method not implemented.");
    }

    handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: Net): void {
        if (request.params?.[apiEndpointKey]) {
            const reqMethod = request.params?.[apiMethodKey] ?? 'get';
            if (reqMethod == 'fetch') {
                NetUtils.fetchRequest(request.params[apiEndpointKey], request, net)
                    .then(response => {
                        event.reply(request.responseChannel, response);
                    }).catch(err => {
                        event.reply(request.responseChannel, {
                            code: 500,
                            data: err
                        });
                    })
            } else {
                (reqMethod == 'post' ? NetUtils.postRequest(request.params[apiEndpointKey], request, net) : NetUtils.getRequest(request.params[apiEndpointKey], request, net))
                    .then(response => {
                        event.reply(request.responseChannel, response);
                    }).catch(err => {
                        event.reply(request.responseChannel, {
                            code: 500,
                            data: err
                        });
                    })
            }
        }
    }

}