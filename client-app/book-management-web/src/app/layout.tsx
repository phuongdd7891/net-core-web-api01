import "@/assets/css/satoshi.css";
import "@/assets/css/style.css";
import { Metadata } from "next";

export const metadata: Metadata = {
  title: "Book Management Web",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {

  return (
    <html lang="en">
      <body>
        {children}
      </body>
    </html>
  );
}
