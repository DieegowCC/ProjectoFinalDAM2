import type { Metadata } from "next";
import { Inter, Special_Gothic_Expanded_One, Merriweather } from 'next/font/google';
import "./globals.css";
import { cn } from "@/lib/utils";

const merriweatherHeading = Merriweather({subsets:['latin'],variable:'--font-heading'});


// 1. Configure Inter
// 'subsets: ["latin"]' ensures we only load the standard english characters, keeping the file size small.
const inter = Inter({
  subsets: ['latin'],
  display: 'swap', // Ensures text remains visible during font load
  variable: '--font-inter', // Optional: creates a CSS variable for Tailwind usage
});

// 2. Configure Special Gothic Expanded One
const specialGothic = Special_Gothic_Expanded_One({
  weight: '400', // This font usually only comes in one weight
  subsets: ['latin'],
  display: 'swap',
  variable: '--font-special-gothic',
});

export const metadata: Metadata = {
  title: "Sett. Vigilance App",
  description: "Track your users",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" className={cn(inter.variable, specialGothic.variable, merriweatherHeading.variable)}>
      {/* Apply the font classes to the body tag */}
      <body>
        {children}
      </body>
    </html>
  );
}
