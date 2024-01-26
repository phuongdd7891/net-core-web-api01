import { IpcRequest, IpcResponse } from "../../shared/IpcRequest";
import { IpcService } from "../IpcService";
import { apiEndpointKey, apiMethodKey } from "../../electron/IPC/BaseApiChannel";
import { wrapFunction } from "../../utils";

export class BookService extends IpcService {
    private sendBookApi = (req: IpcRequest) => {
        return this.sendApi<IpcResponse>("book", req);
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

    public copyBook(bookId: string, qty: number) {
        let reqParams: any = {
            [apiEndpointKey]: `api/books/copy?id=${bookId}&qty=${qty}`,
            [apiMethodKey]: 'post',
        }
        return this.wrapResponse({
            params: reqParams
        })
    }

    public deleteBooks(ids: string[]) {
        return this.wrapResponse({
            params: {
                [apiEndpointKey]: `api/books/delete-many?ids=${ids.join("&ids=")}`,
                [apiMethodKey]: 'fetch',
                method: 'delete'
            }
        })
    }
}