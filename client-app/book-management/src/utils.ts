import { ClientRequest, Net } from "electron";
import { IpcRequest } from "./shared/IpcRequest";
import { apiEndpointKey, apiHost } from "./electron/IPC/BaseApiChannel";

export const fileToBase64 = (file: File) =>
    new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.readAsDataURL(file);
        reader.onload = () => resolve(reader.result);
        reader.onerror = (error) => reject(error);
    });

export const convertBase64ToBlob = (base64Image: string) => {
    const [baseInfo, baseData] = base64Image.split(',');
    const buff = Buffer.from(baseData, 'base64');
    const mimeString = baseInfo.split(':')[1].split(';')[0];
    const fileName = `.${mimeString.split('/')[1]}`;
    const blob = new Blob([buff], {
        type: mimeString
    });
    return { blob, fileName };
}

export const wrapFunction = <TArgs extends any[], TReturn>(
    targetFunction: (...parameters: TArgs) => TReturn,
  ): (...parameters: TArgs) => TReturn => {
    return (...parameters: TArgs) => {
      return targetFunction(...parameters);
    };
  }

export const formatCategoryPath = (path: string) => {
    return `${path ? path.split('.').map((_, idx) => '.'.repeat(idx + 1)).join('') : ''}`;
}

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
                    try {
                        let responseBodyBuffer = Buffer.concat(buffers);
                        let responseBodyJSON = JSON.parse(responseBodyBuffer.toString());
                        resolve(responseBodyJSON);    
                    } catch (error) {
                        reject(error);
                    }
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
            let reqUrl: string = `${apiHost}/${endpoint}${endpoint.indexOf('?') > 0 ? '&' : '?'}u=${request.params?.["username"] ?? ""}`;
            const netRequest: ClientRequest = net.request({
                url: reqUrl,
                method: 'post'
            })
            netRequest.setHeader("Content-Type", "application/json");
            if (request.params?.body) {
                netRequest.write(JSON.stringify(request.params.body));
            }

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
                    try {
                        let responseBodyBuffer = Buffer.concat(buffers);
                        let responseBodyJSON;
                        if (responseBodyBuffer.length > 0) {
                            responseBodyJSON = JSON.parse(responseBodyBuffer.toString());
                        }
                        resolve(responseBodyJSON ?? { code: 200 });
                    } catch (error) {
                        reject(error);
                    }
                })
            })
            netRequest.on('error', (error) => {
                reject(error.message)
            })
            netRequest.end();
        })
    }

    static fetchRequest(endpoint: string, request: IpcRequest, net: Net) {
        const reqInit: RequestInit = {
            method: request.params?.['method'] ?? 'post',
        };
        if (request.params?.body) {
            const formData = new FormData();
            Object.keys(request.params?.body).forEach(a => {
                if (a == 'FileData') {
                    const blobData = convertBase64ToBlob(request.params?.body[a]);
                    formData.append(a, blobData.blob, blobData.fileName);
                } else {
                    formData.append(a, request.params?.body[a]);
                }
            });
            reqInit.body = formData;
        }
        return new Promise((resolve, reject) => {
            let reqUrl: string = `${apiHost}/${endpoint}${endpoint.indexOf('?') > 0 ? '&' : '?'}u=${request.params?.["username"] ?? ""}`;
            net.fetch(reqUrl, reqInit).then(
                async (res) => {
                    resolve(await res.json())
                }).catch(err => {
                    reject(err.message)
                });
        })
    }

    static getUrlQuery(data: any): string {
        let str: string[] = [];
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

export const channels = {
    appExit: "app-exit",
    loaderShow: "loader-show",
    openFile: "wd",
    dialog: "dialog",
    message: "msg",
    menu: "menu",
    menuLogout: "menu-logout",
    notify: "notify",
    userStore: "user-store",
    setUserStore: "set-user-store",
    userNotifications: "user-notifications",
    setUserNotifications: "set-user-notifications"
}

export const storeKeys = {
    userStore: "user-store",
    userNotification: "user-notification"
}