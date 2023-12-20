import { ClientRequest, Net } from "electron";
import { IpcRequest } from "src/shared/IpcRequest";
import { BaseApiChannel, apiHost } from "../BaseApiChannel";

export class LoginApiChannel extends BaseApiChannel {

    constructor() {
        super("login");
    }

    handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: Net): void {
        let buffers: Buffer[] = [];
        
        const netRequest: ClientRequest = net.request({
            url: `${apiHost}/api/Operations/login`,
            method: 'post'
        })
        netRequest.setHeader("Content-Type", "application/json");
        netRequest.write(JSON.stringify({
            "UserName": "username",
            "Password": "password"
        }))
        netRequest.on('response', (response: any) => {
            response.on('data', (chunk: Buffer) => {
                if (response.statusCode != 200) {
                    console.log(`BODY: ${chunk}`)
                }
                buffers.push(chunk);
            })
            response.on('error', (error: any) => {
                console.log(`ERROR: ${JSON.stringify(error)}`)
            })
            response.on('end', async () => {
                let responseBodyBuffer = Buffer.concat(buffers);
                let responseBodyJSON = JSON.parse(responseBodyBuffer.toString());
                // await session.defaultSession.cookies.set({
                //     path: '/',
                //     domain: 'localhost',
                //     url: 'http://localhost/',
                //     name: bookSessionKey,
                //     value: JSON.stringify({
                //         username: responseBodyJSON.Username,
                //         token: responseBodyJSON.Value
                //     })
                // })
                event.reply(request.responseChannel, responseBodyJSON);
            })
        })
        netRequest.end();
    }

}