﻿namespace SailScores.Database.Entities;

public class ChangeType
{
    public Guid Id { get; set; }
    [StringLength(200)]
    public String Name { get; set; }
    [StringLength(2000)]
    public String Description { get; set; }

    public static Guid CreatedId => new Guid("b6c92ed8-1d15-4a1a-977f-6e59bd0160c7");
    public static Guid DeletedId => new("ee49c9c4-d556-4cab-b740-a3baad9c73c9");
    public static Guid ActivatedId => new("153a8b2a-accf-404c-bb39-61db55f5ee1e");
    public static Guid DeactivatedId => new("87533c82-936d-44bb-8055-9292046a7b9e");
    public static Guid PropertyChangedId => new("f2a0b1d4-3c5e-4f8b-9a7c-6d8e5f2b0c3d");
    public static Guid AdminNoteId => new("9b1af84e-8179-4345-9583-2bf741b111bd");
    public static Guid MergedId => new("1C28A4CB-5994-44EC-9B2B-ACEB3036256B");
}