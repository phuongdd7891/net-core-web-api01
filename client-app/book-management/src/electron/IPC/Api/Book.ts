import { BaseApiChannel } from "../BaseApiChannel";

export class BookApiChannel extends BaseApiChannel {

    constructor() {
        super("book");
    }
}

export class BookCategoryApiChannel extends BaseApiChannel {
    constructor() {
        super("book-category")
    }
}