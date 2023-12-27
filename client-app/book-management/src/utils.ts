import { ClientRequest, Net } from "electron";
import { IpcRequest } from "./shared/IpcRequest";
import { apiEndpointKey, apiHost } from "./electron/IPC/BaseApiChannel";

export class NetUtils {
    static getRequest(endpoint: string, request: IpcRequest, net: Net) {
        return new Promise((resolve, reject) => {
            let buffers: Buffer[] = [];
            let reqUrl: string = `${apiHost}/${endpoint}?u=${request.params?.["username"] ?? ""}`;
            if (Object.getOwnPropertyNames(request.params).some(a => a != 'username' && a != apiEndpointKey)) {
                const { username, endpoint, ...queryParams } = request.params;
                reqUrl += '&' + this.getUrlQuery(queryParams);
            }
            const netRequest: ClientRequest = net.request({
                url: reqUrl
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
                    reject(error);
                })
                response.on('end', async () => {
                    let responseBodyBuffer = Buffer.concat(buffers);
                    let responseBodyJSON = JSON.parse(responseBodyBuffer.toString());
                    resolve(responseBodyJSON);
                })
            })
            netRequest.on('error', (error) => {
                reject(error.message)
            })
            netRequest.end();
        })
    }

    static postRequest(endpoint: string, request: IpcRequest, net: Net) {
        return new Promise((resolve, reject) => {
            let buffers: Buffer[] = [];
            let reqUrl: string = `${apiHost}/${endpoint}?u=${request.params?.["username"] ?? ""}`;
            const netRequest: ClientRequest = net.request({
                url: reqUrl,
                method: 'post'
            })
            netRequest.setHeader("Content-Type", "application/json");
            netRequest.write(JSON.stringify(request.params));            
            netRequest.on('response', (response) => {
                response.on('data', (chunk: Buffer) => {
                    if (response.statusCode != 200) {
                        console.log(`BODY: ${chunk}`)
                    }
                    buffers.push(chunk);
                })
                response.on('error', (error: any) => {
                    reject(error);
                })
                response.on('end', async () => {
                    let responseBodyBuffer = Buffer.concat(buffers);
                    let responseBodyJSON; 
                    if (responseBodyBuffer.length > 0) {
                        responseBodyJSON = JSON.parse(responseBodyBuffer.toString());
                    }
                    resolve(responseBodyJSON ?? { code: 200 });
                })
            })
            netRequest.on('error', (error) => {
                reject(error.message)
            })
            netRequest.end();
        })
    }

    static getUrlQuery(data: any): string {
        let str = [];
        for (let p in data)
            if (data.hasOwnProperty(p) && data[p] !== '') {
                if (Array.isArray(data[p])) {
                    data[p].forEach((e: any) => {
                        if (e) {
                            str.push(encodeURIComponent(p) + "=" + encodeURIComponent(e));
                        }
                    })
                } else {
                    if (data[p] !== null && data[p] !== undefined && data[p] !== '') {
                        str.push(encodeURIComponent(p) + "=" + encodeURIComponent(data[p]));
                    }
                }
            }
        return str.join("&");
    }
}