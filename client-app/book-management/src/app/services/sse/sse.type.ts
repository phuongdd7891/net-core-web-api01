export type SseMessage = {
    id: string,
    message: string,
};
export function checkIsSseMessage(value: any): value is SseMessage {
    if(value == null) {
        return false;
    }
    if("id" in value &&
        "message" in value &&
        typeof value["id"] === "string" &&
        typeof value["message"] === "string") {
        return true;
    }
    return false;
}