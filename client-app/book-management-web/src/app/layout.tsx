"use client";
import Loader from "@/components/Common/Loader";
import { useEffect, useState } from "react";
import "@/assets/css/satoshi.css";
import "@/assets/css/style.css";

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    setTimeout(() => setLoading(false), 1000);
  }, []);

  return (
    <html lang="en">
      <body>
        {loading ? <Loader /> : children}
      </body>
    </html>
  );
}
