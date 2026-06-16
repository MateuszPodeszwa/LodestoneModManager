-- CreateSchema
CREATE SCHEMA IF NOT EXISTS "public";

-- CreateTable
CREATE TABLE "Supporter" (
    "id" TEXT NOT NULL,
    "patreonUserId" TEXT NOT NULL,
    "fullName" TEXT,
    "email" TEXT,
    "imageUrl" TEXT,
    "patronStatus" TEXT,
    "tierTitle" TEXT,
    "currentlyEntitledCents" INTEGER NOT NULL DEFAULT 0,
    "lifetimeSupportCents" INTEGER NOT NULL DEFAULT 0,
    "betaOverride" BOOLEAN,
    "keyGenCount" INTEGER NOT NULL DEFAULT 0,
    "lastKeyGenAt" TIMESTAMP(3),
    "firstSeenAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "lastLoginAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "Supporter_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "KeyGeneration" (
    "id" TEXT NOT NULL,
    "supporterId" TEXT NOT NULL,
    "holder" TEXT NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "KeyGeneration_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "Supporter_patreonUserId_key" ON "Supporter"("patreonUserId");

-- CreateIndex
CREATE INDEX "Supporter_patronStatus_idx" ON "Supporter"("patronStatus");

-- CreateIndex
CREATE INDEX "KeyGeneration_supporterId_idx" ON "KeyGeneration"("supporterId");

-- CreateIndex
CREATE INDEX "KeyGeneration_createdAt_idx" ON "KeyGeneration"("createdAt");

-- AddForeignKey
ALTER TABLE "KeyGeneration" ADD CONSTRAINT "KeyGeneration_supporterId_fkey" FOREIGN KEY ("supporterId") REFERENCES "Supporter"("id") ON DELETE CASCADE ON UPDATE CASCADE;

