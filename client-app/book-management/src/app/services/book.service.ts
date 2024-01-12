import { IpcRequest, IpcResponse } from "../../shared/IpcRequest";
import { IpcService } from "../IpcService";
import { apiEndpointKey, apiMethodKey } from "../../electron/IPC/BaseApiChannel";
import { wrapFunction } from "../../utils";

export class BookService extends IpcService {
    private sendBookApi = (req: IpcRequest) => {
        return this.sendApi<IpcResponse>("book", req).then(res => ({...res, success: res.code == 200}));
    }
    private wrapResponse = wrapFunction(this.sendBookApi);

    public listBooks() {
        return this.wrapResponse({
            params: {
                [apiEndpointKey]: 'api/books'
            }
        })
    }

    public getBook(id: string) {
        return this.wrapResponse({
            params: {
                [apiEndpointKey]: `api/books/${id}`
            }
        })
    }

    public deleteBook(id: string) {
        return this.wrapResponse({
            params: {
                [apiEndpointKey]: `api/books/${id}`,
                [apiMethodKey]: 'fetch',
                method: 'delete'
            }
        })
    }

    public createBook(data: any) {
        let reqParams: any = {
            [apiEndpointKey]: 'api/books',
            [apiMethodKey]: 'fetch',
            body: data
        }
        return this.wrapResponse({
            params: reqParams
        })
    }

    public updateBook(bookId: string, data: any) {
        let reqParams: any = {
            [apiEndpointKey]: `api/books/${bookId}`,
            [apiMethodKey]: 'fetch',
            method: 'put',
            body: data
        }
        return this.wrapResponse({
            params: reqParams
        })
    }
}