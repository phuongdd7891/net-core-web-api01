import { ActionResponse } from "@/actions";
import { useCallback, useState } from "react";

export function useAction<T>(
    action: (...args: any[]) => Promise<ActionResponse<T>>
) {
    const [isLoading, setIsLoading] = useState(false);
    const [data, setData] = useState<T | undefined>(undefined);
    const [err, setErr] = useState<any>(null);

    const excuteAction = useCallback(async (...arg: Parameters<typeof action>) => {
        setIsLoading(true);
        setErr(null);
        try {
            const result = await action(...arg);
            if (result.success) {
                setData(result.data);    
            } else {
                setErr(result.error);
            }
            return result;
        } finally {
            setIsLoading(false);
        }
    }, [action]);

    return { isLoading, excuteAction, data, err };
}