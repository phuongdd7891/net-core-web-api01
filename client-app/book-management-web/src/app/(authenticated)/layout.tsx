import DefaultLayout from "@/components/Layouts/DefaultLayout";

export default function AuthenticatedLayout({ children }: any) {
    return (
        <DefaultLayout>
            {children}
        </DefaultLayout>
    );
}