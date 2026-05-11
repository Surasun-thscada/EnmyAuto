export interface SceneBreakdown {
  scene_number: number;
  image_prompt: string;
  voiceover_script: string;
  duration_seconds: number;
}

export interface StoryboardScript {
  title: string;
  scenes: SceneBreakdown[];
  captions: string;
  hashtags: string[];
}

export interface GenerateStoryboardRequest {
  productName: string;
  category: string;
}

export interface GenerateStoryboardResponse {
  storyboardId: string;
  script: StoryboardScript;
}

export const STORYBOARD_CATEGORIES = [
  { value: 'Cartoon',        label: 'Cartoon'        },
  { value: 'Drama',          label: 'Drama'          },
  { value: 'Comedy',         label: 'Comedy'         },
  { value: 'Travel',         label: 'Travel'         },
  { value: 'Horror',         label: 'Horror'         },
  { value: 'FairyTale',      label: 'Fairy Tale'     },
  { value: 'ProductReview',  label: 'Product Review' },
] as const;

export type StoryboardCategory = typeof STORYBOARD_CATEGORIES[number]['value'];
