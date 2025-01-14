'use server';

import { actionGet } from ".";

export async function getBooks(req: { skip: string, limit: string }) {
    const res = await actionGet<any>('books', req);
    return res;
}