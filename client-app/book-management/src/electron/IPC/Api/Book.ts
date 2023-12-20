import { ClientRequest, Net } from "electron";
import { IpcRequest } from "src/shared/IpcRequest";
import { BaseApiChannel, apiHost } from "../BaseApiChannel";

export class BookApiChannel extends BaseApiChannel {

    constructor() {
        super("book");
    }

    handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: Net): void {
        let buffers: Buffer[] = [];
        
        const netRequest: ClientRequest = net.request({
            url: `${apiHost}/api/books?u=${request.params["username"]}`
        })
        netRequest.setHeader("Content-Type", "application/json");
        netRequest.on('response', (response) => {
            response.on('data', (chunk: Buffer) => {
                if (response.statusCode != 200) {
                    console.log(`BODY: ${chunk}`)
                }
                buffers.push(chunk);
            })
            response.on('error', (error: any) => {
                event.reply(request.responseChannel, error);
            })
            response.on('end', async () => {
                let responseBodyBuffer = Buffer.concat(buffers);
                let responseBodyJSON = JSON.parse(responseBodyBuffer.toString());
                event.reply(request.responseChannel, responseBodyJSON);
            })
        })
        netRequest.end();
    }

}