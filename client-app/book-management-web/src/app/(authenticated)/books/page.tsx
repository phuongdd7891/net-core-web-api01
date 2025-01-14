'use client';

import { getBooks } from "@/actions/book";
import { AlertError } from "@/components/Alerts/AlertError";
import Loader from "@/components/Common/Loader";
import { useAction } from "@/hooks/useAction";
import { useEffect } from "react";

export default function Books() {
  const { isLoading, excuteAction, data, err } = useAction(getBooks);
  useEffect(() => {
    excuteAction({ skip: 0, limit: 100, createdFrom: '2024-06-05 00:00:00' });
  }, []);

  return (
    <>
      {isLoading && <Loader/>}
      {err && (
        <AlertError>
          {err}
        </AlertError>
      )}
      {data && (
        <div>
          {data.data.list.map((x: any) => {
            return <div key={x.id}>{x.name}</div>;
          })}
        </div>
      )}
    </>
  );
}
