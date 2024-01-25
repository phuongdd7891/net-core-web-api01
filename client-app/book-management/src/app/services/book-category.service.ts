import { IpcRequest, IpcResponse } from "../../shared/IpcRequest";
import { IpcService } from "../IpcService";
import { apiEndpointKey, apiMethodKey } from "../../electron/IPC/BaseApiChannel";
import { wrapFunction } from "../../utils";

export class BookCategoryService extends IpcService {
    private sendCategoryApi = (req: IpcRequest) => {
        return this.sendApi<IpcResponse>("book-category", req);
    }
    private wrapResponse = wrapFunction(this.sendCategoryApi);

    public listCategories() {
        return this.wrapResponse({
            params: {
                [apiEndpointKey]: 'api/book-category'
            }
        })
    }

    public getCategory(id: string) {
        return this.wrapResponse({
            params: {
                [apiEndpointKey]: `api/book-category/${id}`
            }
        })
    }

    public createCategory(data: any) {
        let reqParams: any = {
            [apiEndpointKey]: 'api/book-category',
            [apiMethodKey]: 'post',
            body: data
        }
        return this.wrapResponse({
            params: reqParams
        })
    }
}