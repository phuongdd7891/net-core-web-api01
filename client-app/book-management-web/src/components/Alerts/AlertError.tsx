
export function AlertError({ children }: any) {
    return (
        <div className="flex w-full rounded-[10px] border-l-6 border-red-light bg-red-light-5 px-3 py-4 mb-4 dark:bg-[#1B1B24] dark:bg-opacity-30 md:p-5">
            <div className="w-full">
                <ul>
                    <li className="text-[#CD5D5D]">
                        {children}
                    </li>
                </ul>
            </div>
        </div>
    );
}