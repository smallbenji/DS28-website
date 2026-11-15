export interface RegistrationHistory {
    presignups: { campYear: number; category: string; participantCount: number; guestCount: number }[];
    signups: { campYear: number; category: string; dayIndex: number; participantCount: number }[];
}

export interface RegistrationHistoryRow {
    category: string;
    presignup: number | null;
    guests: number | null;
    signup: number | null;
    change: number | null;
    days: Record<number, number>;
}

export function registrationHistoryRows(history: RegistrationHistory): RegistrationHistoryRow[] {
    const categories = [...new Set([
        ...history.presignups.map(row => row.category),
        ...history.signups.map(row => row.category)
    ])];
    const order = ['family', 'beaver', 'wolf', 'junior', 'trop', 'senior', 'rover', 'leader'];
    return categories.sort((a, b) => {
        const aIndex = order.indexOf(a);
        const bIndex = order.indexOf(b);
        return (aIndex < 0 ? order.length : aIndex) - (bIndex < 0 ? order.length : bIndex) || a.localeCompare(b);
    }).map(category => {
        const presignup = history.presignups.find(row => row.category === category);
        const signups = history.signups.filter(row => row.category === category);
        const signup = signups.find(row => row.dayIndex === 0)?.participantCount ?? null;
        return {
            category,
            presignup: presignup?.participantCount ?? null,
            guests: presignup?.guestCount ?? null,
            signup,
            change: presignup != null && signup != null ? signup - presignup.participantCount : null,
            days: Object.fromEntries(signups.filter(row => row.dayIndex > 0).map(row => [row.dayIndex, row.participantCount]))
        };
    });
}

export function registrationHistoryTotals(rows: RegistrationHistoryRow[]) {
    function sum(field: 'presignup' | 'guests' | 'signup'): number | null {
        if (!rows.length || rows.some(row => row[field] == null)) return null;
        return rows.reduce((total, row) => total + row[field]!, 0);
    }
    const presignup = sum('presignup');
    const signup = sum('signup');
    return {
        presignup,
        guests: sum('guests'),
        signup,
        change: presignup != null && signup != null ? signup - presignup : null
    };
}
