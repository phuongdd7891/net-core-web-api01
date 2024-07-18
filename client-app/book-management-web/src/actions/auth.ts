'use server';

import { cookies } from "next/headers";
import { actionPost } from ".";

export async function login (req: { username: string, password: string }) {
    const res = await actionPost<any>('Operations/login', req);
    if (res.success) {
        cookies().set('__psession__', JSON.stringify(res.data.data));
    }
    return res;
}

export async function logout () {
    const res = await actionPost<any>('Operations/logout', null);
    if (res.success) {
        cookies().delete('__psession__');
    }
    return res;
}