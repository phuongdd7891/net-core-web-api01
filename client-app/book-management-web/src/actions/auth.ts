'use server';

import { cookies } from "next/headers";
import { actionPost } from ".";
import { sessionKey } from "@/const";

export async function login (req: { username: string, password: string }) {
    const res = await actionPost<any>('Operations/login', req);
    if (res.success) {
        cookies().set(sessionKey, JSON.stringify(res.data.data));
    }
    return res;
}

export async function logout () {
    const res = await actionPost<any>('Operations/logout', null);
    if (res.success) {
        cookies().delete(sessionKey);
    }
    return res;
}