import Signin from "@/components/Auth/Signin";

export default function SignIn() {
    return (
        <div className="container mx-auto md:w-[450px] w-full">
            <div className="rounded-[10px] bg-white shadow-1 dark:bg-gray-dark dark:shadow-card md:mt-5">
                <div className="flex flex-wrap items-center">
                    <div className="w-full">
                        <div className="w-full p-4 sm:p-10.5 xl:p-12">
                            <Signin />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}