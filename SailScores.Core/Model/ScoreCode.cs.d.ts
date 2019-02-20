declare module server {
	interface scoreCode {
		id: any;
		clubId: any;
		scoringSystemId: any;
		name: string;
		description: string;
		/** can be:COD - Use value of ScoreLike to find another code to useFIN+ - competitors who finished this race + FormulaValueSER+ - competitors in this series + FormulaValueAVE - average of all non-discarded racesPLC% - Place + xx% of DNF score (xx is stored FormulaValue)MAN - allow scorer to enter score manually */
		formula: string;
		formulaValue?: number;
		scoreLike: string;
		discardable?: boolean;
		cameToStart?: boolean;
		started?: boolean;
		finished?: boolean;
		preserveResult?: boolean;
		/** Should scoring of other following competitors use this as a finisher ahead? */
		adjustOtherScores?: boolean;
	}
}
