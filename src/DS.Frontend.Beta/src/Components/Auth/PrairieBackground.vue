<template>
    <svg class="prairie" viewBox="0 0 1920 1080" preserveAspectRatio="xMidYMax slice" aria-hidden="true" focusable="false">
        <defs>
            <linearGradient id="prairie-sky" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0" stop-color="#B5DDF6" />
                <stop offset="0.55" stop-color="#D6ECFA" />
                <stop offset="0.72" stop-color="#F3E9D6" />
            </linearGradient>
            <g id="prairie-boot" stroke="#603818" stroke-width="1.5">
                <path d="M-26,2 Q-18,5 -11,2 M-5,2 H3" fill="none" stroke="#70451F" stroke-width="2.6" opacity=".55" />
                <path
                    d="M-13,-33 Q-8,-35 -3,-31 Q2,-35 7,-34 L5,-14 Q5,-9 4,-6 L3,2 H-5 L-5,-2 Q-8,-1 -11,1 Q-17,4 -24,3 Q-29,2 -27,-2 Q-24,-7 -16,-9 Q-11,-11 -11,-16 Z"
                    fill="#A36535"
                />
                <path d="M-26,0 Q-20,2 -14,0 Q-8,-2 -5,-5 Q-1,-7 4,-7" fill="none" stroke="#603818" />
                <path d="M-9,-28 Q-5,-23 -4,-18 Q-1,-22 2,-28" fill="none" stroke="#D09A60" stroke-width="1.1" />
            </g>
        </defs>

        <rect class="prairie-sky" width="1920" height="1080" fill="url(#prairie-sky)" />

        <g class="prairie-sun">
            <circle cx="1530" cy="655" r="120" fill="#F8E4A6" opacity="0.6" />
            <circle cx="1530" cy="655" r="78" fill="#F5D77A" />
        </g>

        <g class="prairie-clouds">
            <g fill="#FFFFFF" opacity="0.75">
                <path d="M150,250 a40,40 0 0 1 70,-25 a55,55 0 0 1 100,10 a35,35 0 0 1 55,15 Z" />
                <path d="M420,380 a30,30 0 0 1 52,-18 a42,42 0 0 1 78,8 a26,26 0 0 1 40,10 Z" />
                <path d="M1480,200 a45,45 0 0 1 80,-28 a60,60 0 0 1 110,12 a38,38 0 0 1 60,16 Z" />
                <path d="M1700,420 a28,28 0 0 1 48,-16 a38,38 0 0 1 70,8 a24,24 0 0 1 36,8 Z" />
            </g>
        </g>

        <g class="prairie-vultures" fill="none" stroke="#7A4A1E" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" opacity="0.55">
            <path d="M1310,300 q12,-10 24,0 q12,-10 24,0" />
            <path d="M1385,335 q9,-8 18,0 q9,-8 18,0" />
        </g>

        <g class="prairie-far-mesas">
            <g>
                <path
                    fill="#E6C9AC"
                    d="M0,770 V715 H70 L95,700 H210 L240,728 L330,735 L360,668 H560 L590,712 L690,722 L720,742 L860,748 L890,705 H1010 L1035,738 L1180,744 L1210,690 H1330 L1355,722 L1430,728 L1460,700 H1600 L1625,735 L1760,740 L1790,708 H1920 V770 Z"
                />
            </g>
        </g>

        <g class="prairie-near-mesas">
            <g>
                <path fill="#D49A70" d="M0,780 V690 L40,688 L60,640 H250 L270,662 L300,664 L322,700 L380,738 L470,770 Z" />
                <path fill="#C2865C" d="M250,640 L270,662 L300,664 L322,700 L380,738 L470,770 L300,770 L290,700 L262,690 Z" />
                <path fill="#D49A70" d="M1380,780 L1440,742 L1500,734 L1525,682 L1550,676 L1570,630 H1790 L1812,668 L1850,672 L1872,705 L1920,712 V780 Z" />
                <path fill="#C2865C" d="M1790,630 L1812,668 L1850,672 L1872,705 L1920,712 V780 H1800 L1808,700 L1798,668 Z" />

                <g class="prairie-cowboy" transform="translate(1585,630) scale(0.5)">
                    <path
                        fill="#5A3210"
                        d="M-28,-11 Q-36,-6 -30,0 L6,0 Q10,0 10,-5 L11,-12 L17,-2 Q20,1 23,-1 Q25,-3 23,-6 L10,-38 Q9,-42 5,-42 L4,-43 Q7,-45 7,-49 L7,-51 L13,-51 Q15,-52 13,-53 L8,-53 Q8,-61 5,-62 Q2,-63 1,-60 Q-1,-63 -4,-62 Q-7,-61 -6,-53 L-11,-53 Q-13,-52 -11,-51 L-5,-51 L-6,-47 Q-5,-43 -2,-43 L-3,-41 Q-5,-40 -6,-37 L-17,-17 Q-19,-13 -15,-12 L-11,-12 L-28,-11 Z"
                    />
                    <path d="M-26,-6 Q-38,-4 -48,6" fill="none" stroke="#5A3210" stroke-width="9" stroke-linecap="round" />
                    <path d="M-51,1 L-44,9 L-51,14 Q-56,15 -57,10 L-55,3 Q-54,0 -51,1 Z" fill="#5A3210" />
                    <path d="M-30,-6 Q-36,6 -40,18" fill="none" stroke="#5A3210" stroke-width="10" stroke-linecap="round" />
                    <path d="M-45,15 L-35,17 L-36,26 L-51,26 Q-55,25 -52,21 Z" fill="#5A3210" />
                </g>
            </g>
        </g>

        <rect class="prairie-plain" x="0" y="760" width="1920" height="320" fill="#E6C08F" />

        <g class="prairie-distant-cactus">
            <g stroke="#5E8C4A" stroke-linecap="round" fill="none">
                <path d="M860,792 V748" stroke-width="10" />
                <path d="M860,776 H850 V762" stroke-width="7" stroke-linejoin="round" />
                <path d="M860,768 H870 V756" stroke-width="7" stroke-linejoin="round" />
            </g>
        </g>

        <g class="prairie-camp">
            <g>
                <g transform="translate(-150,0)">
                    <g stroke="#7A4A1E" stroke-width="4" stroke-linejoin="round">
                        <path d="M1245,830 V700" stroke-linecap="round" />
                        <path d="M1247,704 L1305,719 L1247,734 Z" fill="#B8322A" />
                        <path d="M1150,745 L1210,830 H1090 Z" fill="#F3E3C3" />
                        <path d="M1150,772 L1166,830 H1134 Z" fill="#7A4A1E" />
                    </g>
                    <g transform="translate(1213,852)">
                        <path d="M-26,8 L26,-6" stroke="#5A3210" stroke-width="8" stroke-linecap="round" />
                        <path d="M-26,-6 L26,8" stroke="#7A4A1E" stroke-width="8" stroke-linecap="round" />
                        <path class="prairie-flame prairie-flame--outer" d="M0,-50 C10,-34 22,-24 18,-8 C15,2 -15,2 -18,-8 C-21,-22 -6,-30 0,-50 Z" fill="#B8322A" />
                        <path class="prairie-flame prairie-flame--middle" d="M2,-36 C9,-24 14,-18 11,-8 C8,0 -9,0 -11,-8 C-13,-18 -3,-24 2,-36 Z" fill="#F08A2C" />
                        <path class="prairie-flame prairie-flame--inner" d="M1,-22 C5,-15 7,-11 5,-6 C3,-2 -4,-2 -5,-6 C-6,-11 -2,-15 1,-22 Z" fill="#F5CF3A" />
                    </g>
                </g>

                <g transform="translate(540,850) scale(0.9)">
                    <path d="M-38,-48 C-50,-40 -52,-26 -48,-14" fill="none" stroke="#5A3210" stroke-width="7" stroke-linecap="round" />
                    <path
                        d="M-38,-50 C-20,-58 0,-56 18,-54 C28,-54 40,-46 50,-32 L52,-39 L56,-31 C60,-24 64,-14 66,-8 C68,-3 63,0 59,-2 C55,-5 52,-10 49,-14 C46,-19 42,-24 38,-27 C35,-30 34,-33 34,-36 C34,-34 32,-30 30,-28 L31,0 L25,0 L24,-24 L20,-24 L19,0 L13,0 L14,-26 C0,-24 -14,-24 -24,-26 L-23,0 L-29,0 L-31,-22 L-33,-22 L-37,0 L-43,0 L-41,-28 C-44,-34 -44,-46 -38,-50 Z"
                        fill="#9A5B2A"
                    />
                    <path d="M18,-54 C28,-54 40,-46 50,-32" fill="none" stroke="#5A3210" stroke-width="5" stroke-linecap="round" />
                    <path d="M60,0 l-4,-10 M64,0 l1,-12 M68,0 l5,-9" fill="none" stroke="#5E8C4A" stroke-width="3" stroke-linecap="round" />
                </g>
                <g transform="translate(690,850) scale(-0.9,0.9)">
                    <path d="M-38,-48 C-50,-40 -52,-26 -48,-14" fill="none" stroke="#5A3210" stroke-width="7" stroke-linecap="round" />
                    <path
                        d="M-38,-50 C-20,-58 0,-56 18,-54 C26,-62 32,-76 39,-86 L42,-95 L46,-88 C52,-82 60,-74 65,-67 C68,-62 65,-56 60,-56 C55,-56 50,-59 45,-61 C40,-63 37,-58 36,-52 C35,-46 34,-42 34,-40 C34,-34 32,-30 30,-28 L31,0 L25,0 L24,-24 L20,-24 L19,0 L13,0 L14,-26 C0,-24 -14,-24 -24,-26 L-23,0 L-29,0 L-31,-22 L-33,-22 L-37,0 L-43,0 L-41,-28 C-44,-34 -44,-46 -38,-50 Z"
                        fill="#7A4A1E"
                    />
                    <path d="M18,-56 C26,-62 32,-76 39,-85" fill="none" stroke="#5A3210" stroke-width="5" stroke-linecap="round" />
                </g>

                <path fill="#C8955A" d="M0,885 C300,855 600,860 900,880 C1200,900 1500,850 1920,870 V1080 H0 Z" />

                <g transform="translate(1168 857) scale(1.12)" stroke="#7A4A1E" stroke-width="4" stroke-linejoin="round">
                    <path d="M0,-68 L52,0 H-52 Z" fill="#EADBB8" />
                    <path d="M0,-44 L13,0 H-13 Z" fill="#7A4A1E" />
                </g>

                <g stroke-linecap="round" stroke-linejoin="round">
                    <path d="M435,917 C495,918 628,912 747,906" fill="none" stroke="#754821" stroke-width="7" opacity=".18" />
                    <path d="M444,900 L744,887 L744,892 L444,907 Z" fill="#805226" />
                    <path d="M538,907 L539,859 M638,903 L638,856 M738,900 L738,854" fill="none" stroke="#855628" stroke-width="6" />
                    <path d="M438,913 L440,861" fill="none" stroke="#754821" stroke-width="9" />
                    <path d="M435,867 L743,858 L746,861 L746,865 L434,874 Z" fill="#87572B" />
                    <path d="M435,867 L743,858 L746,861 L435,871 Z" fill="#AC773F" />
                    <path d="M482,884 Q598,880 744,872 L744,878 Q610,885 454,892 L434,893 L434,887 Z" fill="#87572B" />
                    <path d="M475,887 Q614,882 731,875" fill="none" stroke="#AC773F" stroke-width="1" />
                    <path d="M442,875 L441,907" fill="none" stroke="#A46F37" stroke-width="1.5" />
                </g>

                <g transform="translate(-20 0)" fill="none" stroke-linecap="round" stroke-linejoin="round">
                    <path :d="lassoOuter" stroke="#775029" stroke-width="3.8" />
                    <path :d="lassoOuter" stroke="#D5AE72" stroke-width="2.1" />
                    <path :d="lassoInner" stroke="#775029" stroke-width="3.9" />
                    <path :d="lassoInner" stroke="#E1BD82" stroke-width="2.2" />
                </g>

                <path d="M433,866 Q440,865 448,864 L452,868 L434,872 Z" fill="#563315" opacity=".32" />
                <g transform="translate(439 866) rotate(-19)" stroke="#5A3210" stroke-width="1.2" stroke-linejoin="round">
                    <path d="M-9,-2 C-10,-6 -10,-12 -8,-15 Q-7,-17 -3,-16 Q0,-16 2,-19 Q5,-21 8,-17 Q11,-13 13,-3 Z" fill="#9A5B2A" />
                    <path d="M-8,-13 Q-5,-14 -2,-13 Q2,-13 5,-16" fill="none" stroke="#BC8047" />
                    <path d="M-9,-6 Q0,-5 11,-9 L13,-4 Q1,0 -9,-2 Z" fill="#603818" />
                    <path d="M-19,-5 Q-12,-1 -3,-2 Q13,-3 22,-10 Q19,-2 10,2 Q3,5 -4,4 Q-14,3 -19,-5 Z" fill="#A46835" />
                    <path d="M-12,0 Q-3,3 6,0" fill="none" stroke="#C18A50" stroke-width=".8" />
                </g>

                <g transform="translate(499 917) rotate(-4) scale(.70)" stroke-linecap="round" stroke-linejoin="round">
                    <use href="#prairie-boot" transform="translate(15 -4)" />
                    <use href="#prairie-boot" />
                </g>
            </g>
        </g>

        <g class="prairie-tumbleweed-track" transform="translate(0 945)">
            <g class="prairie-tumbleweed">
                <ellipse cx="0" cy="39" rx="32" ry="6" fill="#7A4A1E" opacity="0.18" />
                <g class="prairie-tumbleweed-bounce">
                    <g class="prairie-tumbleweed-spin" fill="none" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="0" cy="0" r="36" fill="#C49565" fill-opacity="0.18" stroke="#8A6233" stroke-width="2.2" stroke-opacity="0.55" />
                        <path d="M25,22 C-13,3 5,19 -29,7 M-12,27 C-8,-20 -3,25 26,-23 M25,-16 C-10,11 1,-11 -10,30 M14,27 C20,4 13,-8 -13,-31 M-31,-9 C11,-12 2,17 33,-5 M31,1 C5,15 9,10 -28,-14 M24,-20 C30,-4 -21,15 -14,29 M19,-29 C-15,12 12,-17 -25,22" stroke="#6E4A27" stroke-width="2.4" />
                        <path d="M-23,19 C1,11 13,-1 27,-14 M3,30 C-18,-20 -16,-2 -5,-29 M11,-30 C23,-22 11,22 6,30 M-15,31 C14,-17 -26,-3 12,-29 M31,-7 C10,-18 -25,-1 -7,32 M-15,-29 C1,-12 5,-14 31,14 M-25,18 C-16,-8 -22,-3 27,-21 M-3,-30 C8,-15 24,3 26,14" stroke="#A47A47" stroke-width="2" />
                        <path d="M32,3 L43,7 M25,21 L40,17 M12,30 L25,35 M-20,25 L-25,35 M-28,16 L-41,14 M-31,-11 L-42,-11 M-17,-27 L-29,-32 M0,-32 L7,-43 M28,-17 L35,-26" stroke="#6E4A27" stroke-width="2" />
                    </g>
                </g>
            </g>
        </g>

        <g class="prairie-foreground">
            <g>
                <g stroke-linecap="round" stroke-linejoin="round" fill="none">
                    <path d="M262,1010 V760" stroke="#5E8C4A" stroke-width="46" />
                    <path d="M250,905 H212 Q196,905 196,889 V830" stroke="#5E8C4A" stroke-width="32" />
                    <path d="M274,862 H306 Q322,862 322,846 V795" stroke="#5E8C4A" stroke-width="32" />
                    <path d="M254,770 V1000 M270,770 V1000" stroke="#4A7339" stroke-width="3" />
                </g>

                <g transform="translate(196 816)" stroke="#984A3D" stroke-width="1.15" stroke-linejoin="round">
                    <path
                        d="M0,3 C-9,3 -15,-2 -18,-8 C-12,-8 -8,-6 -5,-3 C-8,-8 -8,-12 -8,-15 C-3,-12 -1,-9 0,-6 C1,-11 4,-14 7,-17 C9,-12 8,-8 6,-4 C10,-8 14,-9 18,-10 C16,-3 9,2 0,3 Z"
                        fill="#CF655A"
                    />
                    <path d="M0,4 C-7,4 -12,0 -14,-4 C-9,-4 -6,-3 -3,-1 C-4,-5 -3,-8 0,-11 C3,-8 4,-5 3,-2 C7,-5 11,-6 15,-6 C13,-1 7,4 0,4 Z" fill="#EA9381" />
                    <path d="M0,4 Q-4,1 -3,-3 Q0,-5 3,-3 Q4,1 0,4 Z" fill="#F3C46E" stroke-width=".8" />
                </g>

                <g stroke-linecap="round" stroke-linejoin="round" fill="none">
                    <path d="M1690,1010 V820" stroke="#5E8C4A" stroke-width="38" />
                    <path d="M1700,920 H1730 Q1744,920 1744,906 V860" stroke="#5E8C4A" stroke-width="26" />
                    <path d="M1683,828 V1000 M1697,828 V1000" stroke="#4A7339" stroke-width="3" />
                </g>

                <path d="M1443,1010 V834 Q1443,828 1449,828 H1453 Q1459,828 1459,834 V1010 Z" fill="#C49565" stroke="#5A3210" stroke-width="4" stroke-linejoin="round" />
                <g transform="rotate(-3 1440 875)">
                    <path d="M1350,849 H1528 L1552,875 L1528,901 H1350 Z" fill="#C49565" stroke="#5A3210" stroke-width="4" stroke-linejoin="round" />
                    <circle cx="1361" cy="859" r="2.6" fill="#5A3210" />
                    <circle cx="1361" cy="891" r="2.6" fill="#5A3210" />
                    <text
                        x="1446"
                        y="886"
                        text-anchor="middle"
                        font-family="Rockwell, 'Roboto Slab', Georgia, serif"
                        font-weight="700"
                        font-size="28"
                        letter-spacing="3"
                        fill="#5A3210"
                    >TYDAL</text>
                </g>

                <path fill="#B07A45" d="M0,995 C320,965 620,975 960,990 C1300,1005 1600,972 1920,980 V1080 H0 Z" />

                <g fill="none" stroke="#8A6233" stroke-width="4" stroke-linecap="round">
                    <path d="M90,1010 l-10,-26 M96,1010 l2,-30 M102,1010 l12,-24" />
                    <path d="M820,1004 l-9,-22 M826,1004 l1,-26 M832,1004 l10,-20" />
                    <path d="M1460,1000 l-10,-24 M1466,1000 l2,-28 M1472,1000 l11,-22" />
                    <path d="M1860,1004 l-8,-20 M1865,1004 l1,-24 M1870,1004 l9,-18" />
                </g>
            </g>
        </g>
    </svg>
</template>

<script setup lang="ts">
const lassoOuter = "M459,866 C455,865 453,868 452,873 L448,886 C444,901 450,909 461,908 C472,907 477,899 475,888 L473,874 C472,868 469,865 466,866 M460,907 C459,912 461,916 466,916 C469,916 470,912 469,910";
const lassoInner = "M462,866 C459,868 459,876 457,882 C454,891 454,900 460,902 C467,904 472,896 471,890 C470,884 468,878 468,872 Q467,867 465,866";
</script>

<style scoped>
.prairie {
    display: block;
    width: 100%;
    height: 100%;
}

.prairie-tumbleweed {
    animation: prairie-tumbleweed-roll 22s linear 2s infinite both;
}

.prairie-tumbleweed-bounce {
    animation: prairie-tumbleweed-bounce 1.3s infinite;
}

.prairie-tumbleweed-spin {
    transform-box: fill-box;
    transform-origin: center;
    animation: prairie-tumbleweed-spin 1.1s linear infinite;
}

@keyframes prairie-tumbleweed-roll {
    from {
        transform: translateX(-200px);
    }

    55%,
    to {
        transform: translateX(2120px);
    }
}

@keyframes prairie-tumbleweed-bounce {
    0%,
    100% {
        transform: translateY(0);
        animation-timing-function: ease-out;
    }

    35% {
        transform: translateY(-36px);
        animation-timing-function: ease-in;
    }

    70% {
        transform: translateY(0);
    }
}

@keyframes prairie-tumbleweed-spin {
    to {
        transform: rotate(360deg);
    }
}

.prairie-flame {
    transform-box: fill-box;
    transform-origin: center bottom;
    animation: prairie-flame-flicker ease-in-out infinite alternate;
}

.prairie-flame--outer {
    animation-duration: 0.9s;
}

.prairie-flame--middle {
    animation-duration: 0.65s;
    animation-delay: -0.3s;
}

.prairie-flame--inner {
    animation-duration: 0.45s;
    animation-delay: -0.2s;
}

@keyframes prairie-flame-flicker {
    0% {
        transform: scale(1, 1) skewX(0deg);
    }

    30% {
        transform: scale(0.94, 1.1) skewX(-4deg);
    }

    65% {
        transform: scale(1.05, 0.92) skewX(3deg);
    }

    100% {
        transform: scale(0.97, 1.06) skewX(-2deg);
    }
}

@media (prefers-reduced-motion: reduce) {
    .prairie-tumbleweed {
        display: none;
    }

    .prairie-flame {
        animation: none;
    }
}
</style>
