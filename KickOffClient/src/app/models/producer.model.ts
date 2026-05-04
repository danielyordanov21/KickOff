export interface Producer {
  id: string
  userName: string
  email: string
  role: string

  profilePictureUrl?: string

  field?: string
  degree?: string
  institution?: string

  bio?: string
  producerMessage?: string

  phone?: string

  projectIds?: string[]
  publicationIds?: string[]

  followerIdsP?: string[]
  followingIdsP?: string[]
}