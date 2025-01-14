'use server';

import { sessionKey } from '@/const';
import { cookies } from 'next/headers';

const baseUrl = 'http://localhost:5253/api';
export type ActionResponse<T> = {
    success: boolean,
    data?: T,
    error?: any
};

const getSessionData = () => {
    if (cookies().has(sessionKey)) {
        const sessionData = JSON.parse(cookies().get(sessionKey)?.value || "{}");
        const token = sessionData.token || "";
        const username = sessionData.username || "";
        return { token, username };
    }
    return null;
};

export async function actionPost<T>(endpoint: string, req: any) : Promise<ActionResponse<T>> {
    try {
        const sessionData = getSessionData();
        const headerAuth = {
            Authorization: `bearer ${sessionData?.token || ""}`
        };
        
        const result = await fetch(`${baseUrl}/${endpoint.startsWith('/') ? endpoint.substring(1) : endpoint}?u=${sessionData?.username}`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
                ...headerAuth
            },
            body: JSON.stringify(req)
        });
        const data = await result.json();
        return {
            success: result.ok,
            data: result.ok ? data : null,
            error: result.ok ? null : data.data || 'Something wrong'
        };
    } catch (error: any) {
        return {
            success: false,
            error: error.message
        }
    }
}

export async function actionGet<T>(endpoint: string, req: Record<string, any>) : Promise<ActionResponse<T>> {
    try {
        const sessionData = getSessionData();
        const headerAuth = {
            Authorization: `bearer ${sessionData?.token || ""}`
        };
        let url = `${baseUrl}/${endpoint.startsWith('/') ? endpoint.substring(1) : endpoint}?u=${sessionData?.username}`;
        Object.keys(req).forEach(a => {
            if (Array.isArray(req[a])) {
                Array(...req[a]).forEach(x => {
                    url += `&${a}=${encodeURIComponent(x)}`;
                });
            } else {
                url += `&${a}=${encodeURIComponent(req[a])}`;
            }
        });
        const result = await fetch(url, {
            method: 'get',
            headers: {
                "Content-Type": "application/json",
                ...headerAuth
            },
        });
        const data = await result.json();
        return {
            success: result.ok,
            data: result.ok ? data : null,
            error: result.ok ? null : data.data || 'Something wrong'
        };
    } catch (error: any) {
        return {
            success: false,
            error: error.message
        }
    }
}